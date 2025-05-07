Scene contains the blue noise generator.

To generate new asset:
1. Click "Play"
2. Click on the "AssetGenerator" game object and make sure that the settings are correct
3. Right click on the "Blue Noise Asset Generator" script and click "Generate Blue Noise Assets"
4. Wait until "Assets created" appears in the console



Settings explained:
- Asset Dimensions: Determines the bounds in which points will be generated
- Min Distance: Minimal distance between points
- k: The k value determines how often each point tries to find a viable point in its surrounding. The higher, the denser the resulting asset, but at an increased computation time
- Use Growing K: if checked, will multiply the k value by the already found points. This increases the chance of finding additional points towards the end, best set to true unless computation time becomes a problem
- Number of Assets to Generate: Will generate that many assets in one go. This shouldn't increase the computation time unless the number is higher than the number of available worker threads
- Path to Sample Point Asset: Path where the assets will be placed. Will create folders that do not exist
- Sample Point Asset Name: Name of the base asset. Assets that are created in one go will have their number as a suffix. The generator will override assets with the same name that already exist at the target location
