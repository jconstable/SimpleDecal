# SimpleDecal
A simple solution to adding decals to a Unity Scene.

![Three decal projectors on crates](https://github.com/jconstable/SimpleDecal/blob/master/Images/DecalDemo.PNG)

Decals can be added to an object by using the context menu in the hierarchy, and creating a new Decal Project under "3D Objects".
![Creating a new decal projector](https://github.com/jconstable/SimpleDecal/blob/master/Images/ContextMenu.PNG)

Any material can be used in the decal's MeshRenderer. It is simply new geometry, copied from the intersecting geometry and clipped to the Decal Projector space.

Clipping work is done via a job, and is extremely fast. The job itself is allocation-free, though Mesh generation has a cost.
