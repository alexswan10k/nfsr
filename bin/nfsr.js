var fsharp = require('fsharp');

var script = fsharp({
	executable: "./script.fsx",
	args: Array.slice(arguments)
});