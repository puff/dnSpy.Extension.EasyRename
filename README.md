dnSpy.Extension.EasyRename
==========================

A simple [dnSpy] extension for easily renaming members.

# Usage
Simply right click a member and click the `Rename Member` in the context menu. It will be under the `Edit <xxx>...` option.

## Features
- [x] Rename overridden methods automatically.
  - See [issues](https://github.com/puff/dnSpy.Extension.EasyRename/issues) for current limitations.
- [x] Rename type when renaming a constructor method.

### Warnings
* There is no undo function when renaming with this extension.
* There is no `unsaved changes` prompt when you rename with this extension.

These are both part of the [AsmEditor](https://github.com/dnSpyEx/dnSpy/tree/master/Extensions/dnSpy.AsmEditor) extension, but the service used for these features is not public, so it cannot be implemented here.
* This was built using [**dnSpyEx v6.3.0**](https://github.com/dnSpyEx/dnSpy/releases/tag/v6.3.0), it may not work if you use another version of dnSpy.

## Installation
Download the [latest release](https://github.com/puff/dnSpy.Extension.EasyRename/releases/latest) for your version of [dnSpy] (`net48` or `net6.0-windows`) and extract it to the `dnSpy/bin/Extensions` directory. \
You may need to create the `Extensions` folder if it doesn't already exist. You can also create a subdirectory there for this extension to organize your extensions folder.

[dnSpy]:https://github.com/dnSpyEx/dnSpy