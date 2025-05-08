# Object Pooling

These scenes contain examples for object pooling. The simple example contains a very basic version that allows a singular object to switch between states when selected as a target.
The complex scenes contain two examples how this behaviour can be combined with compound objects to also use the probability value provided by the spawn point.


## Overview

### Base User Study Handler

The BaseUserStudyHandler replaces the DistractorTaskManager. It manages the individual trials and kicks off the point generation and the spawning of objects.

Instead of spawning the assets itself, it hands the assets to the GenericAssetGenerator that uses object pooling to generate the assets. For that purpose a prefab / object with a specific component needs to be specified.


### GenericAssetGenerator

The GenericAssetGenerator provides an abstract base class for the object pooling. For an example implementation look at the ColorSwitchAssetGenerator. A concrete implementation has to provide an implementation of the OnReleaseObject, OnGetObject and SetProbabilty methods


#### OnReleaseObject

This method is called when the object is returned to the pool. This might include setting the object to inactive

#### OnGetObject

This method is called when the object is taken from the pool. This might include setting the object active.  

#### SetProbabilty

This method is called at the same time as OnGetObject and hands the probabilty value to the object that is taken out of the pool. This can be useful to select a specific object that should be spawned if some objects should appear more often than others. Look at the complex example for a usage of this method.

### DistractorGroupTrialEnumerator

To simplify moving from trial group to trial group, the functionality was moved to this enumerator. As long as trials are left the enumerator calculates the next group on MoveNext and returns the current group and a group range which is useful if only certain groups should be displayed (also see Differences to DistractorTaskManager below).
It automatically turns around when reaching the end of the spline.

### Differences to DistractorTaskManager

Most notable differences are the maxGroupCountToSpawn setting and the simplified asset selection.

With the maxGroupCountToSpawn a number of groups that should be spawned in addition to the currently active group can be set. This prevents too many objects from being spawned. To activate this feature the spawnAllGroupsAtTheBeginning checkbox has to be unchecked.

Instead of providing a series of assets a singular asset is provided. This removes the ability to provide more than one asset to the asset spawning itself. Instead, if more than one asset should be spawned, all assets would have be combined into a compound asset that then auto selects the correct version inside the AssetGenerator (see Complex Object Pooling Sample).
