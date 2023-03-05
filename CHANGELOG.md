# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.2]
- Fix for missing UIToolkit drawer when IMGUI support was disabled.

## [1.0.1]
- IMGUI support is now optional.  
  You can add IMGUI support by manually referencing the `com.needle.editorpatching` package, mentioned in the README.  
  The Harmony DLL can cause issues in certain versions of Unity, and depending on its version can throw errors when using Burst.  
  If you experience freezing when `com.unity.entities` is installed and you look at the Project Settings, removing that package will fix the issue.

## [1.0.0]
- Initial release. This release follows on from Vertx.SerializeReferenceDropdown, which has been marked readonly.