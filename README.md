# Dicer

Dicer is a package for generating and managing 3d dice of any size. 
It includes creating dice procedurally, performing actual and fake rolls and using it all in 2d via automatic prerendering.

Currently it includes only three types shapes: tetrahedron, trapezohedra and bipyramids, which are not that popular, but allow creating fair dice of any size. 
> Odd numbers are represented through repetition 🤓

## Usage

### Installation

The package can be installed through the UPM by the link `https://github.com/Alexiush/Dicer.git`.

### In 3D (Worldspace)

To create dice with Dicer a `ProceduralDie` component should be added to the GameObject. 
Then you can proceed with setting the parameters of your dice and it's visuals: shape, number of sides, material, sides texture resolution and sources.

There are also other useful components for interactive dice: 
* `DiceCollider` - Managing physics initialization, so it happens after generation
* `DiceThrower` - Creating artificial force and torque to immitate throw and sending events about the throw results
* `DiceRotator` - Rotating dice towards the desired side

The package contains examples of URP materials to use with Dicer's dice, the main point is in using cubemaps for the die material and `_Mask` texture for numbers or any other symbols on sides textures.

### In 2D (UI)

Unity allows using 3D elements in UI. However, it is heavier than 2D and also can't be treated as most UI elements.
Dicer provides prerendering feature that allows to pack the generated die into `UIDieData` config with the sprites for it's sides and rolling animation.
There are also UI components for both legacy UI and UI Toolkit that use this data to show die in 2D.

## Roadmap:

* Popular shapes (DnD dice, platonic shapes)
* Optimal texturing
* Special effects for rolling, throwing, etc (Blur is prioritized)

> In a current state Dicer contains some parts that may be not absolutely correct or pretty underoptimized 😢, Fixing this is also on the roadmap

## Credits

* [CatLikeCoding tutorial on procedural meshes](https://catlikecoding.com/unity/tutorials/procedural-meshes/)
* [NothkeUtils](https://github.com/nothke/unity-utils/blob/master/Runtime/RTUtils.cs) for rendering
* [Dropdown attribute](https://assetstore.unity.com/packages/tools/utilities/dropdown-attribute-180951) - note that Dicer contains slightly modified version of it
