dnSpy.Extension.EasyRename
==========================

A simple [dnSpy](https://github.com/dnSpyEx/dnSpy) extension for easily renaming members.

## Features
- [x] Rename overridden methods automatically.
- [x] Rename type when renaming a constructor method.

### Warnings
* There is no undo function when renaming with this extension.
* There is no `unsaved changes` prompt when you rename with this extension.

These are both part of the [AsmEditor](https://github.com/dnSpyEx/dnSpy/tree/master/Extensions/dnSpy.AsmEditor) extension, but the service used for these features is not public, so it cannot be implemented here.