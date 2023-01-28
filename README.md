# Locus Preset Style For Unity3D

## Introduction

This GitHub repository is for a Unity3D framework that enables developers to easily style their components using a CSS-like approach. The framework utilizes a built-in preset system that allows developers to modify styles by simply dragging and dropping components and making changes in the preset inspector. The stylesheet is a scriptable object that can be easily modified.

## Pros
- Allows for a more organized and efficient way to manage and apply styles to Unity3D components. 
- Allows for easy modification and reuse of styles. 
- Designed for small-scale management of styles and can be especially useful for developers who find it difficult to manage styles using only prefab systems. 
- Can improve the overall design and consistency of a Unity3D project. 
- The framework works only on design time, which means there is no runtime overhead.

# Installation
To install, use Unity3D's built-in Package Manager.(or you can add it into manifest.json manually)

# Getting Started
1. Create a new `PresetStyleSheet` scriptable object. This object will serve as the container for your styles.
<img width="671" alt="image" src="https://user-images.githubusercontent.com/6591432/215271766-eadb3c5a-1a8f-4da0-b2ae-469dfd405bd5.png">

2. In the `PresetStyleSheet`, define a new selector and drag-and-drop a component to associate it with the selector. 
<img width="538" alt="image" src="https://user-images.githubusercontent.com/6591432/215271865-1387b740-2f9b-4bd6-9881-56b5cf5ac854.png">

3. Modify the component's preset as desired using the inspector.
4. Attach a `PresetStyleSheetRoot` component to your root gameobject. This component will be responsible for applying the styles to your components.
<img width="651" alt="image" src="https://user-images.githubusercontent.com/6591432/215271974-ed9aabae-e406-447d-96bb-449d5dc1ffae.png">

5. In the hierarchy, add new style defines by pressing "A" button, or using the context menu option.
<img width="593" alt="image" src="https://user-images.githubusercontent.com/6591432/215272011-7ae12128-b4c2-4f97-8558-cb49060b8e1b.png">

6. The styles will be automatically applied to the associated components in the scene.
7. If you modify the stylesheet after it has been applied, you will need to re-apply the styles using the context menu options: "Apply Preset Style" or "Apply Preset Style Recursive".
<img width="624" alt="image" src="https://user-images.githubusercontent.com/6591432/215272141-3930cb1f-476c-4375-99be-637416d50032.png">


# Using Multiple Sheets
You can use multiple sheets if needed.
1. In the `PresetStyleSheet`, you can add multiple parent sheets. 
<img width="641" alt="image" src="https://user-images.githubusercontent.com/6591432/215272165-14e03d58-6066-4433-895f-c69aea19d638.png">
2. Sheets will be applied in order if there's multiple selector combinations.<br>
**It is important to note that styles in parent sheets will take precedence over styles in child sheets. This allows for flexibility in organizing your styles and making changes to specific subsets of components.**

# Analyze Styles Applied
1. Open the analyzor window using `Window > Preset Style > Show Analyzor Window`.
<img width="393" alt="image" src="https://user-images.githubusercontent.com/6591432/215272203-4f3b673e-ffdc-4d99-b248-5a79daa18679.png">

2. Select one or more gameobjects which you defined styles on.
<img width="748" alt="image" src="https://user-images.githubusercontent.com/6591432/215272225-ac761e61-018b-472f-9c6a-0e5cb7f1d649.png">

3. Alternatively, you can open the window using the component context menu to track only that specific component.
<img width="390" alt="image" src="https://user-images.githubusercontent.com/6591432/215272238-3bf09a3f-911c-4bbc-aa49-989361f4b966.png">

# Beware
The components related this framework will not be saved in build using HideFlags. Do not use it in your code.

## License
[MIT](LICENSE.md)
