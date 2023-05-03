Figma for Unity is a powerful plugin that allows you to deliver top level UI designs straight to Unity Editor.

Disclaimer: The plugin is in experimental state. It was developed for internal use only. If you see how you could contribute and have something to submit, then we would kindly ask you to follow the codestyle. You can find code style description here: <link>

## Overview

* AWESOME GIF FROM JASUR *

Have you ever dreamt about the magical button which would just get the design from UI-tool (Figma) and put it right to the Unity Editor without recreating all elements manually? Here it is - Figma for Unity allows you to put the designs from Figma documents to Unity and do multiple manipulations with UI elements via scripts.  

Features:
- Imports and parses figma pages into uxml/uss assets in Unity
- Do manipulations with UI elements and apply any desired logic via scripts
- Supports around 85% of figma tools (see limitations below)
- All changes done to elements in Figma can be easily fetched in Unity 

## Installation
0. The following packages should be installed:
- "org.nuget.newtonsoft.json": "13.0.1",
- "com.modesttree.unity3dasyncawaitutil": "1.0.5",
- "com.unity.vectorgraphics": "2.0.0-preview.20",
- "com.trackman.commonutils": "3.6.0"

1. Window -> Package Manager -> "+" -> Add package from git URL -> https://github.com/TrackMan/Unity.Package.FigmaForUnity

2. Get your figma PAT
    1. Login at https://www.figma.com/developers/api?fuid=797042793200923967#authentication
    2. Press '+ Get personal access token'
    3. Copy token, find Figma script in the Unity Inspector and paste the token

## How to use

1. Figma side
Figma has the current structure: Document (has a title in the URL) -> Page (where you group several frames) -> Frame (this is what you are going to improt into Unity)
    1. Create frame (the structure could look like MyFigmaDocument/MyFigmaPage/MyFirstFrame)
    2. Copy the title from the URL

2. Unity Side / Inspector
    1. Create variant of Figma prefab
    2. Create essential assets for UI document component:
        1. Create -> UI Toolkit -> Panel Settings Asset
        2. Create -> UI Toolkit -> TSS Theme File
        3. Create -> UI Toolkit -> Text Settings
        4. Assign theme files and text settings to the panel settings asset.
    3. Paste title from figma document over to Title field in Figma script.
    4. Create script through which you want to manipulate with the imported frame

3. Unity Side / Inside of the script
    1. To bind the script to the figma frame, Attributes are used.
        1. Add Uxml attribute. It should be put in front of the class declaration and be written as following: [Uxml("{pageName}/{frameName}")]. Example: [Uxml("MyFigmaPage/MyFirstFrame")]. This attribute binds the figma frame to this script
        2. Create fields and put Query attributes in front of them in order to bind specific UI elements to the fields in the script. Example: [Query("BlackRectangle")] VisualElement blackRectangle;

4. Get back to Unity Editor and press 'Update UI' button. Specify the folder to save UXML if needed. Once update is done, you can launch the game and see imported frame on the screen.

You can find samples in the ~Samples folder.

(to cover: attributes, how paths work, all supported elements, creating of the custom elements, filtering - both scripts and images)

## TODO
Cleanup figma parser. Regex.
Known issues
Roadmap
localization...?

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

