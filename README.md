# Obj2Nav2

Converts arbitrary wavefront .obj files to MGSV compatible .nav2 files.

## Usage

`Obj2Nav2.exe Path\to\mesh.obj`

A file called `output.nav2` will be created in the current working directory.

## Guidelines

The provided mesh should use the same scale and co-ordinate space as the map you are working on (i.e. the in-game co-ordinates should map directly to the .obj). If you have a `.geoms` file for the map, it can be helpful to use https://github.com/oldbanana12/GeomsParser to create an obj of some of the map geometry and then use that to build your navmesh around.

Provided meshes should be as simple as possible in terms of edge count. The application does its own subdivision of the mesh to produce the necessary data structures for the .nav2 file.