# Documentation

## Overview

## Package contents

## Installation instructions


Install the package via the Unity Package Manager using the git url:

https://docs.unity3d.com/Manual/upm-ui-giturl.html

## Requirements

The package is currently using Unity 6000.0.25f1. Technically, there shouldn't be any reason why a previous version should not work as well, but I have not tested it.

## Limitations


## Workflows

### Project Setup

1. Add a new layer (by default the package expects layer 3) and call it "DistractorCloud"
2. Setup a normal scene for the MagicLeap with the OpenXR camera rig
3. Add the DistractorCloudManager-prefab to the scene and unpack the root prefab. This will leave the child prefab "AdditionalCameraSetup" as a packed prefab
4. Move the child prefab to the main camera
5. On the main camera, under Camera > Stack add the "DistractorOverlayRenderCamera" to the camera stack.
6. Remove the layer created in step 1 from the Culling Mask of the main camera (Note: if the layer is not layer 3, the layer has to be set manually as the only Culling Mask layer of the Overlay Camera and as the layer on the "Closest Spline Point Generation Component" that is attached to the "DistractorCloudManager" Game Object).

(Optional)
Locate the input mappings at Packages > Distractor Clouds > Runtime > InputMappings > CustomMagicLeapOpenXRInput and enable them as the project default. The package should work without, but the mappings are just a modified copy of the default MagicLeapOpenXRInput mappings.


## Advanced topics

## Reference

## Samples

The package contains one sample scene with the completed setup of the Project Setup section

## Tutorials
