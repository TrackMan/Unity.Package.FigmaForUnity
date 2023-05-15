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
Token is being kept in the raw format.

## How to use

### Figma Prefab
![image](https://github.com/anton-trackman/test/blob/main/FigmaPrefab.png)

* **Panel Settings.** Important asset that makes it possible for Unity to display UXML-file based UI in the Game view
* **Source Asset.** UXML asset that defines the structure of the UI
* **Sort Order.** Defines the order of the UXML if there are multiple instances of the Panel Settigns in the project
_
* **Title.** The title that you copy from figma document URL
* **Asset.** UXML asset which represents the same asset from UI document script (see avove)
* **Update Buttons.** You can update UI either with downloading all images or not
* **De-root and Re-order Hierarchy.** This will reorder all your frames according to each tlement's RootOrder property
* **Filter by Path.** Enabling this flag allows to download only those UI frames, whose scripts are attached to the prefab. Otherwise, all UI represented in the figma document will be downloaded
* **Additional Fonts Directories.** Here you can add paths to the fonts which are used in UI

### Frame Script Anatomy
```csharp
[Uxml("SamplePage/SampleFrame", UxmlDownloadImages.Everything, UxmlElementTypeIdentification.ByElementType)]
public class SampleFrame : Element
{
    #region Fields
    [Query("SampleButton", Clicked = nameof(Test))] Button sampleButton;
    #endregion

    #region Methods
    void Test() => Debug.Log("Test");
    #endregion
}
```
As you can see the simple frame script consist of: Uxml attribute, Frame class which inherits from Element, fields with Query attributes

### Attributes
**Uxml attribute**
```csharp
public UxmlAttribute(string root = default, UxmlDownloadImages imageFiltering = UxmlDownloadImages.Everything, UxmlElementTypeIdentification typeIdentification = UxmlElementTypeIdentification.ByName, params string[] preserve)
```
* **root.** Represents the path to the frame. Example: "SamplePage/SampleFrame"
* **imageFiltering.** Which type images should be downloaded. Example: UxmlDownloadImages.Everything
* **typeIdentification.** By which way element types should be defined. Example: UxmlElementTypeIdentification.ByElementType
* **preserve**. Root folders of additional content frames. Example: "SamplePage/Content"
  
**Query attribute**
```csharp
public QueryAttribute(string path, Clicked = nameof(Test), StartRoot = true, StopRoot = true, Nullable = true, Template = "SampleTemplate", UseTrickleDown = TrickleDown.TrickleDown)
```
* **path.** Represents the path to the element
* **Clicked.** Defines the method which should be executed

### Element Types
* VisualElement
* Button
* ScrollView

### Custom Elements
As addition to standard elements, you can create your own custom elements
```csharp
public class CustomButton : Button
{
    public new class UxmlFactory : UxmlFactory<CustomButton> { }

    #region Fields
    [Query("CustomButtonLabel")] Label label;
    #endregion
}
```

## TODO
- Cleanup figma parser. Regex
- Known issues
- Roadmap
- Localization

## Limitations
- Main Frame should have a unique name
- Colors and Icons that we will reuse should be put into a separate frame (as rectangles)
- Vectors cannot be invisible, cannot have Image Fill, should be grouped into Union where possible
- Fills can only have one Fill per element
- Auto-Layout cannot have stroke borders
- Cannot mix horizontal center and vertical center elements within the same parent (will fallback to center-center)
- Circles should be implemented through rectangles instead of ellipses (for better visuals) ??? check

## Best practices
- Keep it stupid simple
- Hierarchy of elements should be as simple as possible and have proper naming (not Frame 4815162342)

# E N J O Y !

