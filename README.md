# SerializeReference Dropdown
### `[ReferenceDropdown]`
Decorates a `[SerializeReference]` field, adding a type selection dropdown and optional features.  

| Argument                               | Description                                                                                                                       |
|----------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| **Type** type                          | Optional type constraint                                                                                                          |
| **ReferenceDropdownFeatures** features | Optional feature selection flags:<br/>- Remove the constrained type label (Type)<br/>- Remove "Set to null" from the context menu |

  
![ReferenceDropdown Example](Documentation~/ReferenceDropdownExample.gif)

> **Note**  
> `ReferenceDropdown` supports **property drawers**, **decorators**, and  **UIToolkit**.  

IMGUI support uses IL injection to make multiple modifications to the editor DLL.  
UIToolkit support uses stateful DecoratorDrawer hacks.  
Other implementations often don't support property drawers due to the complexity of these approaches.

---

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z42ZYHB)

## Installation

> **Warning**  
> **This package requires Unity 2020.3+**  
> In versions **below 2021** `ReferenceDropdown` may draw incorrectly when used with property drawers that nest property fields.

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.vertx.serializereference-dropdown

To add it the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.vertx
            com.needle
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.vertx.serializereference-dropdown`
- click <kbd>Add</kbd>  
</details>

<details>
<summary>Add from GitHub | <em>not recommended, no updates through UPM</em></summary>

You can also add it directly from GitHub on Unity 2019.4+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/vertxxyz/Vertx.SerializeReferenceDropdown.git`
- click <kbd>Add</kbd>  
  **or**
- Edit your `manifest.json` file to contain `"com.vertx.SerializeReferenceDropdown": "https://github.com/vertxxyz/Vertx.SerializeReferenceDropdown.git"`,

⚠️ SerializeReferenceDropdown has a dependency on [Editor Patching](https://github.com/needle-tools/editorpatching) and [Vertx.Utilities](https://github.com/vertxxyz/Vertx.Utilities) so ensure they are referenced into your project to use this package successfully. ⚠️

To update the package with new changes, remove the lock from the `packages-lock.json` file.
</details>