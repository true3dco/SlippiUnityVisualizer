Any Questions? Hit us up in discord: https://discord.gg/PnF2EW8

# Slippi Unity Visualizer
This project reads a special version of `.slp` files frame by frame.
As each frame is read, it is turned into a human readable format that you can then use to take some action in Unity.
 
## Special Version of SLP
The structure of a generic `slp` is not compatible with the Unity Serialization library.
Use the repo here to convert slp into the appropriately structured JSON: https://github.com/true3dco/SlippiFactory/blob/main/README.md

## Setup
Animations and models are not provided. 
Models need to be `.fbx` and animations need to be `.fbx` clips each stored in their own `fbx`

## Getting started
First get all the animations and models you want to use. You can create these in any animation software.

1. Attach the SlippiParser component onto any top level gameobject in your scene
2. Create a top level game object called `World`
3. Create a child game object of `World` called `Player1`
4. Create a child game object of `World` called `Player2`
5. Wire up these game objects in the `SlippiParser` component
6. Hit play

## Structure of the code
The best way to understand the codebase is to read `SlippiParser`

`SlippiAnimationPlayer` is used for testing animations in a specific folder against a predetermined model. 

`SlippiLookupTable` - turns `slp` file codes into human readable values

`SlippiModels` - Reads data from the `slp` file through Unity's Serialization system

`SlippiParser` - This game object that gets attached to the scene that powers unity playback
 
## Manual Mode
This is an option in the component in `SlippiParser.cs`
Manual mode lets you control the frame iteration with the left and right arrow keys.

## Uploading as a 3D video
Join the hyperplane discord and follow the instructions for posting in there: https://discord.gg/PnF2EW8
