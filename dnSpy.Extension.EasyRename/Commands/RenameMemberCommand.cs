using System;
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace EasyRename.Commands;

[ExportMenuItem(Header = "Rename Member", Group = Constants.ContextMenuEditGroup, Order = 10)]
public sealed class RenameMemberCommand : MenuItemBase 
{
    private readonly IDocumentTabService _documentTabService;

    [ImportingConstructor]
    public RenameMemberCommand(IDocumentTabService documentTabService)
    {
        _documentTabService = documentTabService;
    }
    
    public override void Execute(IMenuItemContext context) 
    {
        var member = GetMemberDef(context);
        if (member is null) return;

        var isConstructor = member is MethodDef { IsConstructor: true };
        var defaultText = isConstructor ? member.DeclaringType.Name : member.Name;
        var title = "Rename ";
        if (member.IsEventDef)
            title += "Event";
        else if (member.IsFieldDef)
            title += "Field";
        else if (member.IsMethodDef && !isConstructor)
            title += "Method";
        else if (member.IsPropertyDef)
            title += "Property";
        else if (member.IsTypeDef || isConstructor)
            title += "Type";

        var newName = MsgBox.Instance.Ask<string?>("Name", defaultText, title,
            null, s => string.IsNullOrEmpty(s) ? "Cannot be an empty string" : string.Empty);
        
        if (string.IsNullOrEmpty(newName) || (isConstructor ? member.DeclaringType.Name : member.Name) == newName)
            return;

        if (isConstructor)
            member.DeclaringType.Name = newName;
        else
        {
            if (member is MethodDef { IsVirtual: true } method)
            {
                var sigComparer = new SigComparer(0, method.Module);
                foreach(var t in method.Module.GetTypes().Where(x => HasBaseType(x, method.DeclaringType.GetScopeType()!, false)))
                {
                    foreach (var m in t.Methods)
                    {
                        if (!m.IsVirtual || !m.Name.Equals(method.Name) || !sigComparer.Equals(method.MethodSig, m.MethodSig))
                            continue;

                        m.Name = newName;
                        break;
                    }
                }
            }
        }

        // Update MemberRef rows
        var memberRefs = new MemberFinder().FindAll(member.Module).MemberRefs.Keys?
            .Where(x => new SigComparer(0).Equals(x, member));
        if (memberRefs is not null)
            foreach (var memberRef in memberRefs)
                memberRef.Name = newName;
            
        member.Name = newName;
        if (member.IsTypeDef)
            member.Module.ResetTypeDefFindCache();
        
        // Refresh UI
        var memberNode = _documentTabService.DocumentTreeView.FindNode(member);
        if (memberNode is not null)
        {
            memberNode.TreeNode.RefreshUI();
            _documentTabService.RefreshModifiedDocument(memberNode.GetDocumentNode()?.Document!);
        }
    }

    /// <summary>
    /// Checks whether a type implements a specific base type or interface.
    /// </summary>
    /// <param name="type">The type to check on.</param>
    /// <param name="baseType">The base type or interface to check for.</param>
    /// <param name="implicit">Whether to check the type itself against the base type as well.</param>
    /// <returns>Whether the type implements a specific base type or interface.</returns>
    private static bool HasBaseType(ITypeDefOrRef type, ITypeDefOrRef baseType, bool @implicit = true)
    {
        if (!@implicit && type == baseType)
            return false;

        var isInterface = baseType is TypeDef { IsInterface: true };

        var bt = type.GetScopeType();
        while (bt is not null)
        {
            var resolved = bt.Resolve();
            if (isInterface)
            {
                if (resolved is not null && resolved.Interfaces.Any(i => i.Interface.GetScopeType()!.Equals(baseType)))
                    return true;
                
                bt = bt.GetBaseType().GetScopeType();
                continue;
            }
            
            if (bt.Equals(baseType)) 
                return true;
            
            bt = bt.GetBaseType().GetScopeType();
        }

        return false;
    }

    private static IMemberDef? GetMemberDef(IMenuItemContext context) 
    {
        if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
            return context.Find<TextReference>()?.Reference as IMemberDef;
        
        if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
        {
            var nodes = context.Find<TreeNodeData[]>();
            if (nodes?.Length is not 1)
                return null;

            var node = nodes[0] as IMDTokenNode;
            return node?.Reference as IMemberDef;
        }
        return null;
    }

    // Only show this if selecting a valid member definition
    public override bool IsVisible(IMenuItemContext context) => GetMemberDef(context) is not null;
    public override bool IsEnabled(IMenuItemContext context) => GetMemberDef(context) is not null;
}