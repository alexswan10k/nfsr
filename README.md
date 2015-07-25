# Node FSharp Script Runner
Note : this is a work in progress and is still unstable. Expect API's to change.
### What is it?

nfsr is a script runner/resolver for .fsx files using the benefits of npm linking and packages 
to allow resolving complex dependencies. No longer do you have to worry about exactly
where fsi.exe/fsc.exe is, if it is in your path, and where your scripts are. nfsr provides a
tree walking algorithm to find and execute your scripts for you on demand.

##### What does this mean?
* No solution/project file required
* No Visual Studio required (although it is highly recommended as this has best intellisense support)
* Easier Dependency resolution

### Getting started
To install the runner, you will need nodejs installed and in the path. Then:
```
npm install nfsr -g
```

To create a list of all the available scripts in scope which can be executed via nfsr:
```
nfsr list
```

To execute a script test.fsx by recursing through both locally installed and globally installed packages:
```
nfsr test
```

You can now install more scripts:
```
npm install nfsr-package-one -g
npm install nfsr-package-two
... etc
```
### Consuming dependencies
To set up a working directory:
```
npm init
```
You can now use npm to load dependencies you may wish to reference in your script, or call via nfsr:
```
npm install nfsr-sample-library --save
```

We can create a references file from the installed dependencies:
```
nfsr create-refs
```

### Thats all great, but what when I decide I want to use my "libraries" in a real program?
The fsharp compiler actually covers this, to which a wrapper is provided, "compile".

To create an executable of your script (no parameters currently supported):
```
nfsr compile script.fsx
```
Or perhaps you want to completely decouple from the F# environment
```
nfsr compile script.fsx --standalone
```

You can make binaries too
```
nfsr compile library.fsx --target:library
```

As this is simply a thin wrapper around fsc.exe, all commands you could issue to fsc are theoretically available.

See the [msdn documentation]( https://msdn.microsoft.com/en-us/library/dd233171.aspx) for a full list.


### Why npm and not Paket/nuget?
Paket is a powerful package manager, but it currently has no support for resolving specifically script dependencies recursively.
There is also no distinction between a "script" (something you execute) and a "library" (something referenced by an executable script).
Npm provides a convention for this (all packages contain a lib and a bin folder).
Npm naturally resolves all dependencies in a predictable folder structure compatible with the way .fsx 
scripts interact with #load, thus allowing you to have reasonable confidence that when you `npm restore`, 
your dependencies will be where you expect them to be.

Paket also has no central repository, so all your script links will be pretty cryptic. 
Npm provides a way to abstract a link such as https://github.com/someuser/some-complicated-name.git to a simple name, 
and more importantly search and index these.

This allows you to use relative paths to set up complex dependency trees:
* a.fsx(ref:"node_modules\packageB\lib\libB1.fsx")
	* node_modules
		* packageB
			* bin
				* nfsx-command.fsx(ref:"..\lib\libB2.fsx")
			* lib
				* libB1.fsx(ref:"node_modules\packageC\lib\libC1.fsx")
				* libB2.fsx(ref:"node_modules\packageD\lib\libD1.fsx")
			* node_modules
				* packageC
					* lib
						* libC1.fsx
				* packageD
					* lib
						* libD1.fsx

Note: It is known that npm package consolidation for complex hierarchies breaks this. Possible fix would be to create proxy files on install that point to the consolidated file (WIP).


### Things to note			
When creating a package, it should probably be prefixed by "nfsr-" to prevent confusion with other packages
	
It is recommended that all executable scripts are stored in the bin folder, and all libraries
are stored in lib. Libraries should not have any side effects, simply providing API's for scripts to consume. 
Do not create public api's in executable scripts(bin), as there will be no way to load these without unintended sideeffects.

All libraries are effectively loaded inside their own module. Make sure everything you do not want to expose is either made private or wrapped in a private module.

There is currently no way to make a whole library script internal. 
For the time being it is recommended to prefix script with '_', however this may need further consideration.

Another thing to be aware of is when you create an executable script which is to be run through nfsr, 
it is likely the script will not be executed from the path where it is contained.
It is therefore important to use `System.Environment.CurrentDirectory` when getting the executing path instead of `__SOURCE_DIRECTORY__`.

### Possible future enhancements
* Improvement of tree walking algorithm to support order of prescedence and caching
* Paket integration (any npm package with a packet.dependencies should auto paket install). 
	This would allow for better support of arbitrary .fsx files in gists, along with true
	nuget integration.
* Less noisy output
* Less process wrapping (starting new fsi instances is expensive)
* Proxy files for consolidated dependencies
* Unix support - shell gateway script as opposed to batch file
* Some kind of metadata system for the list command

### What was the drive behind this?

I created this due to being fustrated at how difficult it was to get started with FSharp 
as a general purpose scripting tool. 
FSharp as a scripting language is in many ways the most ideal language there is. 
The language is easy to reason with, has a tremendous type system, has an excellent standard library, and has an elegant minimalist syntax.
After using requirejs for some time, the conventional .fsx file mess embedded in a flat vs project with all my other .fs files felt very inflexible and brittle.

The things that fustrated me most were:
* No way to resolve fsi.exe/fsc.exe without hacking around in the path variable on every computer
* No easy way to share script dependencies in a reusable way
* No easy way to compile a script hierarchy into some useful binary or executable

It is my hope that this project tackles some of these, or at the very least inspires someone else to do a better job.