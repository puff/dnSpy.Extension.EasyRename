using System;
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
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
                foreach(var t in method.Module.GetTypes().Where(x => IsDerived(x, method.DeclaringType.GetScopeType()!, false) || IsOverriddenFrom(x, method, sigComparer)))
                {
                    foreach (var m in t.Methods)
                    {
                        if (!IsOverride(m, method, t, sigComparer))
                            continue;

                        m.Name = newName;
                        break;
                    }
                }
            }
            
            member.Name = newName;
        }

        var moduleDocNode = _documentTabService.DocumentTreeView.FindNode(member.Module)!;
        _documentTabService.DocumentTreeView.TreeView.RefreshAllNodes();
        _documentTabService.RefreshModifiedDocument(moduleDocNode.Document);
    }

    private static bool IsOverride(MethodDef method, IMethod @base, ITypeDefOrRef baseType, SigComparer sigComparer)
    {
        return method.IsVirtual &&
               (method.Name.Equals(@base.Name) || $"{baseType.Name}.{method.Name}".Equals(@base.Name)) &&
               sigComparer.Equals(@base.MethodSig, method.MethodSig);
    }
    
    /// <summary>
    /// Checks if <paramref name="method"/> is overridden from <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type to check overrides.</param>
    /// <param name="method">The method to check if its overridden.</param>
    /// <param name="sigComparer">Optional signature comparer to compare method signatures for checking overrides.</param>
    /// <returns>Whether the <paramref name="method"/> is overridden from a method in <paramref name="type"/>.</returns>
    private static bool IsOverriddenFrom(TypeDef type, MethodDef method, SigComparer sigComparer = new())
    {
        if (!method.IsVirtual || type.Equals(method.DeclaringType) || !IsDerived(method.DeclaringType, type, false))
            return false;

        foreach (var m in type.Methods)
        {
            if (!IsOverride(m, method, type, sigComparer))
                continue;
            
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Checks whether a type implements a specific base type or interface.
    /// </summary>
    /// <param name="type">The type to check on.</param>
    /// <param name="baseType">The base type or interface to check for.</param>
    /// <param name="implicit">Whether to check the type itself against the base type as well.</param>
    /// <returns>Whether the type implements a specific base type or interface.</returns>
    private static bool IsDerived(ITypeDefOrRef type, ITypeDefOrRef baseType, bool @implicit = true)
    {
        if (!@implicit && type == baseType)
            return false;

        // Checks base types and interfaces of type to see if it is from baseType
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