# Simple Object Pooling

Scene contains the example for using the simple object pooling setup.


## Setup

Setup consists of 2 main scripts:

- The CustomUserStudyHandler (inheriting from BaseUserStudyHandler)
- The DistractorUserStudySetup

It also utilizes the SearchAreaHandler and RecenterPathComponent.

The CustomUserStudyHandler inherits from the abstract BaseUserStudyHandler but only has to implement what should happen when a target gets selected or deselected. In this example, on selection the sphere changes its color to red and afterwards it changes back to white.
