FigmaForUnity is a powerful plugin that allows you to deliver top level UI design documents straight to Unity!

Disclaimer: Experimental. It was developed for internal use only. If submit - follow the codestyle, please. You can find it here: ...

## Overview

// gif //

// short description

// full description

Feature:
- Imports and parses figma pages into UXML/USS assets in Unity
- 

## Installation
Window -> Package Manager -> "+" -> Add package from git URL -> https://github.com/TrackMan/Unity.Package.FigmaForUnity
// dependencies ? //
Adding PAT

## How to use
Create design in the figma.
Click update in Unity Editor.

Full cycle:
1. Figma
2. Import
3. Frame scripts
attributes
How paths work
All types of supported elements: VisualElement, Button, ScrollView
Overview of FrameElement?
Short video.. with creating simple button.
Custom Elements - How to create (see sample)
How does Filtering work? (images and scripts).
4. Maintaining

fonts
Add samples

examples?

## TODO
Cleanup figma parser. Regex.
Known issues
Roadmap
localization...?

## Limitations
guidelines

Review this:
	Main Frame should have a unique name
	Colors and Icons that we will reuse should be put into a separate frame (as rectangles)
	Vectors
	cannot be invisible
	cannot have Image Fill
	should be grouped into Union where possible
	Fills can only have one Fill per element
	Auto-Layout cannot have stroke borders
	Cannot mix horizontal center and vertical center elements within the same parent (will fallback to center-center)
	Circles should be implemented through rectangles instead of ellipses (for better visuals)
	If possible use instances of master components as much as possible
	Hierarchy of elements should be as simple as possible and have proper naming (not Frame 4815162342)
	All elements that are not used should be completely removed
	Code Guidelines
	Group elements with StartRoot where possible
	Remove all custom code like SetBorders, everything should come from Figma