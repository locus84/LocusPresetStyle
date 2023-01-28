# Locus Preset Style For Unity3D

## Introduction

This GitHub repository is for a Unity3D framework that enables developers to easily style their components using a CSS-like approach. The framework utilizes a built-in preset system that allows developers to modify styles by simply dragging and dropping components and making changes in the preset inspector. The stylesheet is a scriptable object that can be easily modified.

## Pros
- Allows for a more organized and efficient way to manage and apply styles to Unity3D components. 
- Allows for easy modification and reuse of styles. 
- Designed for small-scale management of styles and can be especially useful for developers who find it difficult to manage styles using only prefab systems. 
- Can improve the overall design and consistency of a Unity3D project. 
- The framework works only on design time, which means there is no runtime overhead.

# Getting Started
1. Create a new `PresetStyleSheet` scriptable object. This object will serve as the container for your styles.
2. In the `PresetStyleSheet`, define a new selector and drag-and-drop a component to associate it with the selector. 
3. Modify the component's preset as desired using the inspector.
4. Attach a `PresetStyleRoot` component to your root gameobject. This component will be responsible for applying the styles to your components.
5. In the hierarchy, add new style defines using the context menu option.
6. The styles will be automatically applied to the associated components in the scene.
7. If you modify the stylesheet after it has been applied, you will need to re-apply the styles using the context menu options: "Apply Preset Style" or "Apply Preset Style Recursive".

# Using Multiple Sheets
You can use multiple sheets if needed.
1. In the `PresetStyleSheet`, you can add multiple parent sheets. 
2. Sheets will be applied in order if there's multiple selector combinations.
It is important to note that styles in parent sheets will take precedence over styles in child sheets. This allows for flexibility in organizing your styles and making changes to specific subsets of components.

# Analyze Styles Applied
1. Open the analyzor window using `Window > Preset Style > Show Analyzor Window`.
2. Select one or more gameobjects which you defined styles on.
3. Alternatively, you can open the window using the component context menu to track only that specific component.

# Beware
The components related this framework will not be saved in build using HideFlags. Do not use it in your code.

## License
[MIT](LICENSE.md)