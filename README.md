# WaveFunctionCollapseGenerator3D
 
This is a 3d implementation of the Wave Function Collapse algorithm for generating randomised levels/spaces from a set of pre-made modules and a set of rules (patterns) about how these modules can connect to eachother. The algorithm is named after the quantum phenomenon it resembles. 

## Theory

The quantum phenomenon describes a wave particle in a superstate collapsing to a single state when observed. Similarly, the algorithm works by supposing each point to be generated as being in a 'superstate' of all possible modules when the generation begins. As the algorithm progresses, points are sequentially 'collapsed' into their final module shapes; that is to say: one of it's possible modules is picked at random as the final module. Which point to collapse at any given stage in the loop is decided by which point has the lowest 'entropy', i.e. the fewest viable module possibilites. When the algorithm starts, all points have equal entropy, so one is chosen at random.
Once a point is collapsed, its neighbouring points are checked (in my implementation this is a 3x3x3 cube centred around the point excluding any that fall outside of the generating area). For each possible module, it checks if this module can connect to the point that has just been collapsed. For example, a road module can connect to another road module if they both run east-to-west. However if one runs north-to-south, they cannot connect and this module is then removed from the possibilities for the neighbouring point. Once all neighbouring points have been checked and any invalid modules removed as possibilities, the algorithm iteratively repeats the process by checking the neighbours of the neighbours.
Once all points have been updated to reflect the last collapsed point, the loop begins again and the point with the lowest entropy (usually a neighbour of the last collapsed point) is collapsed and neighbouring points are again updated. This continues until all points have been collapsed.

Further reading on the quantum phenomenon can be found [here](https://en.wikipedia.org/wiki/Wave_function_collapse). My main source of reference for the application of the principle to generating random spaces was [this](https://www.youtube.com/@PaulMerrellResearch) youtube channel, the owner of which, Paul Merrell, is doing on-going research into this and similar algorithms, including a more well-developed earlier iteration of his of a similar process called 'Model Synthesis'. Among other interesting things, he explains how this algorithm works in detail and how it can be applied to any space that maps to a mathematical graph (a set of points and their connections), not just simple grids.

## Purpose

This was a research project to determine the viability of this method for generating random levels for a separate game project. To this end, it is not as generic as it could be. For example, it is constrained to work with a solid floor and air-based ceiling. The module set and their connections can however be customised, with the sample one developed for the type of levels wanted for the project.

Main goals:
- Interesting levels that are fun to explore and play in. For the game in question this meant sightlines for projectile combat and lots of verticality and movement options for fast-paced gameplay.
- Levels that portray a certain level of verisimilitude with real places.
- Results that differ visually and spacially to a meaningful extent.
- Importantly: performance.


-- things to improve - convert file reading to json, speed improvements, async and parallel processing to this end, improve module sets to avoid holes.

## How To Use

In Unity under the Generator object, input a level size to generate. A good starting size is x: 6, y: 5, z: 6. Run the project. Nothing will appear to happen until the level has finished generating, at which point the generated level is loaded into the scene. Asynchronous generating is perhaps a future goal.

## Custom Levels

initially, the collection of patterns, known as the grammar, had to be manually specified using a text file or the like. This was difficult and time consuming, hard to read and debug, and required a lot of tedious busywork. For example, for any given pattern, each 90 degree rotation about the y axis had to be separately encoded.
After working on this for a bit I devised a better method. This was to extract the grammar procedurally from a piece of sample terrain. The implementation is somewhat admittedly a little hacky due to how it has to interact with Blender, but could be streamlined in future for ease of use.

1. First, create a set of modules. These need to be 3d models in Blender that fit within a cube of constant size (my modules are each 3x3x3 blender 'units'). Name each of these appropriately (road1, bridge, roof_corner3, etc) and ** make sure they have no rotations applied**. Then, arrange them as they should naturally connect in a sample level. If rotating a piece like a road, make sure the rotation is recorded on the Blender object. Try to cover as many possibilities as you can. 
This is what this looks like:

![image](https://github.com/user-attachments/assets/a87a2252-c5b9-4949-8080-729df2a9796a)

The right shows all modules, including all rotations. The left is the actual sample that will be processed to generate the patterns that will then be used in the algorithm. How big it should be depends on how many modules you have, and therefore how many possible patterns they can make.

2. Select all modules in the sample you want to record the patterns of, then run the attached python script. This will go through each selected object, record it's name, it's location and it's rotation (y axis only).
  
3. Then, in Unity, tick the 'generate patterns' box and provide the location of the level folder (use an existing version for guidance) and run Unity. This iterates through the resultant array and records all possible patterns. Patterns are not rotation agnostic so the script also rotates the array through all 4 directions to find any potentially missed patterns.

4. Finally, populate the Generator script with the level folder name and all the module models from Unity (.fbx files are best).

## Results

Here is a sample level generated:

![image](https://github.com/user-attachments/assets/75943a04-229d-4fb1-b713-96f3cceee979)

Note the occasional smaller cuboid in a space. This is a placeholder model for when no matching pattern could be found. In order to avoid these, expand the sample to cater for these scenarios. An empty (air) point is also considered a module so bear this in mind.

A smaller (6x5x6) level:

![image](https://github.com/user-attachments/assets/46a76d96-77ca-46a8-9781-2d51321450ff)

And the same level from a prospective player's perspective:

![image](https://github.com/user-attachments/assets/5b71c5bc-2993-4fec-a2ce-8610684ee8f3)

Another small level:

![image](https://github.com/user-attachments/assets/f5e7fdce-d404-4066-ae4c-74ca70974abc)


## Future Iterations

There are a number of improvements to be made.

1. Speed. The current version is already much improved from the first working version and uses all sorts of tricks to do so. Nonetheless it is still slow, probably too slow for practical application in it's current state without excessive loading times.
2. Reliability. Iteration is currently the best method to find and fix gaps in the grammar that leave ugly, empty spots.
3. QoL. Even as a stand alone testing tool this is still quite clunky. It could and should run on a different thread, have some progress visualisation, and the process for creating levels can be streamlined and improved in a number of ways as well.
4. Parallel processing. The feasibility of this is unclear and it is heavily linked to speed, but this would be a major improvement to the algorithm if it could be made to work.
