# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.4]
### Fixed
- Custom names are properly displayed in the assigned field if you target a `AdvancedDropdownAttribute` subclass.

## [1.0.3]
- Made changes to allow for users to fix an exception with Burst manually.
  If you are using 2022.2+ it's advised to **remove the editorpatching package** and just use UIToolkit for all custom editors.
  You can now fix the exception (*Failed to find entry-points*) by copying the `com.needle.editorpatching` package locally to your Packages directory, navigating to `com.needle.editorpatching@version/Editor/Plugins` and renaming the DLL to `0Harmony.dll`. Navigate to the `Editor` folder above and edit `needle.EditorPatching.asmdef` to include a precompiled reference to `"0Harmony.dll"`. 

## [1.0.2]
- Fix for missing UIToolkit drawer when IMGUI support was disabled.

## [1.0.1]
- IMGUI support is now optional.  
  You can add IMGUI support by manually referencing the `com.needle.editorpatching` package, mentioned in the README.  
  The Harmony DLL can cause issues in certain versions of Unity, and depending on its version can throw errors when using Burst.  
  If you experience freezing when `com.unity.entities` is installed and you look at the Project Settings, removing that package will fix the issue.

## [1.0.0]
- Initial release. This release follows on from Vertx.SerializeReferenceDropdown, which has been marked readonly.