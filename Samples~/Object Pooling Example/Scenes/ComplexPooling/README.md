# Complex Pooling

Scene contains the example for using the complex object pooling setup.


## Setup

Setup consists of these main scripts:

- The MultiObjectUserStudyHandler (inheriting from BaseUserStudyHandler)
- The BaseMultiObjectComponent
  - The MultiPrefabComponent
  - The MultiChildObjectComponent
- The MultiObjectAssetGenerator

### MultiObjectUserStudyHandler

The MultiObjectUserStudyHandler inherits from the abstract BaseUserStudyHandler. This handler expects a prefab with a BaseMultiObjectComponent. The two scenes contain two example implementations of this component. One using prefabs, the other child objects.

### BaseMultiObjectComponent

Abstract class that serves as a base for scripts that want to implement more than one child object. The class defines the PickObject method that provides the probability from the sample point asset.
Two example implementations show how an object with child objects or an object with a list of prefabs can perform the same job (MultiChildObjectComponent and MultiPrefabComponent).

#### MultiPrefabComponent

Has a list of prefabs, selects the appropriate prefab based on the probability and then instantiates the appropriate one. It keeps a reference to it and deletes the prefab before spawning a new one should the PickObject method be called again

#### MultiChildObjectComponent

Has a list of child objects (have to be children), selects the approriate child based on the probabilty and disables all other children.

### MultiObjectAssetGenerator

This asset generator is pretty barebones except for the SetProbabilty method where it transmits the probabilty to the BaseMultiObjectComponent.

### State Switching

In this example the spawned objects / child objects also contain the ColorSwitchComponent from the simple pooling example. This allows the combination of different objects and target state switching.
