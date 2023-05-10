Figma for Unity is a powerful plugin that allows you to deliver top level UI designs straight to Unity Editor.

Disclaimer: The plugin is in experimental state. It was developed for internal use only. If you see how you could contribute and have something to submit, then we would kindly ask you to follow the codestyle. You can find code style description here: <link>

## Overview

* AWESOME GIF FROM JASUR *

Have you ever dreamt about the magical button which would just get the design from UI-tool (Figma) and put it right to the Unity Editor without recreating all elements manually? Here it is - Figma for Unity allows you to put the designs from Figma documents to Unity and do multiple manipulations with UI elements via scripts.  

### Features
- Imports and parses figma pages into uxml/uss assets in Unity
- Do manipulations with UI elements and apply any desired logic via scripts
- Supports around 85% of figma tools (see limitations below)
- All changes done to elements in Figma can be easily fetched in Unity 

## Installation
Unity -> Window -> Package Manager -> "+" -> Add package from git URL -> https://github.com/TrackMan/Unity.Package.FigmaForUnity

### Dependencies
- "org.nuget.newtonsoft.json": "13.0.1"
- "com.modesttree.unity3dasyncawaitutil": "1.0.5"
- "com.unity.vectorgraphics": "2.0.0-preview.20"
- "com.trackman.commonutils": "3.6.0"

### Personal Access Token
- Login at https://www.figma.com/developers/api?fuid=797042793200923967#authentication
- Press '+ Get personal access token'
- Copy token, find Figma script in the Unity Inspector and paste the token

## How to use

### Attributes
Attributes

## TODO
Cleanup figma parser. Regex.
Known issues
Roadmap
localization...?
кстати надо написать что мы храним токен в raw формате без криптования

## Limitations
- Main Frame should have a unique name
- Colors and Icons that we will reuse should be put into a separate frame (as rectangles)
- Vectors cannot be invisible, cannot have Image Fill, should be grouped into Union where possible
- Fills can only have one Fill per element
- Auto-Layout cannot have stroke borders
- Cannot mix horizontal center and vertical center elements within the same parent (will fallback to center-center)
- Circles should be implemented through rectangles instead of ellipses (for better visuals) ??? check

## Best practice
- Keep it stupid simple
- Hierarchy of elements should be as simple as possible and have proper naming (not Frame 4815162342)

