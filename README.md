# Node FSharp Script Runner

nfsr is a script runner/resolver for .fsx files using the benefits of npm linking and packages 
to allow easy execution of locally scoped and system wide scripts. No longer do you have to worry about exactly
where fsi.exe/fsc.exe is, if it is in your path, and where your scripts are precisely. nfsr provides a
tree walking algorithm to find and execute your scripts for you on demand simply calling it by name.

### Getting started
To install the runner, you will need nodejs installed and in the path. Then:
```
> npm install nfsr -g
```

To create a list of all the available scripts in scope which can be executed via nfsr:
```
> nfsr list
```

To execute a script test.fsx by recursing through both locally installed and globally installed packages:
```
> nfsr test -testparam1 -testparam2
```

You can now install more scripts:
```
> npm install nfsr-package-one -g
> npm install nfsr-package-two
... etc
```
##### Thats all great but all my scripts are in powershell/bash/batch files. Can i use this?
No problem. nfsr's same tree walking algo can be used to target any of these:

Run a powershell script:
```
> nfsr -p my-powershell-script
```
list all batch files
```
> nfsr list -b
```

find what file is actually being resolved when you run a command
```
> nfsr resolve -p	
```

### Consuming dependencies
To set up a working directory:
```
> npm init
```
You can now use npm to load dependencies you may wish to reference in your script, or call via nfsr:
```
> npm install nfsr-sample-library --save
```

We can create a references file from the installed dependencies:
```
> nfsr create-refs
```

### Dynamic dependencies (experimental)

It is also possible to use the same system to resolve library references dynamically in both local and global scope. 
Rather than specify a path in your script, you can simply specify the library name to 
generate a _DynamicReferences.fsx file using the following command:
```
> nfsr dynamic-refs add <library>
...
> nfsr dynamic-refs remove <library>
```
This will also manage a configuration file "DynamicReferences.txt" allowing you to quickly restore these references on 
another system where the physical files may be in a different place. Only this file should be checked into source control.
This can be done with:
```
> nfsr dynamic-refs refresh
```
The intention is to tie this into ```nfsr restore```

##### Ok, but I dont want to publish a npm package for my stuff, Can i just use a git repo?

There are a couple of options here:
* Bower - can pull code down directly from a git repository
* Paket - Supports both git and Nuget packages

The tree walker will assume a convention explained below, so as long as your files/directories conform, this should just work.

There is also a function provided for restoring packages recursively, 
which will walk all nested folders looking for bower.json or a paket.dependencies. 
When it finds one of these it will issue a ```bower install``` or ```paket install```
```
> nfsr restore
```

### Convention
Everything in a lib folder is assumed to be a non executable library. 
These are ignored by nfsr but picked up by ```nfsr create-refs```(WIP). 
Anything starting with "lib" or ending with "lib" is also considered to be a library.

Everything else is an executable script.

### Thats all great, but what when I decide I want to use my "libraries" in a real program?
The fsharp compiler actually covers this, to which a wrapper is provided, "compile".

To create an executable of your script (no parameters currently supported):
```
> nfsr compile script.fsx
```
Or perhaps you want to completely decouple from the F# environment
```
> nfsr compile script.fsx --standalone
```

You can make binaries too
```
> nfsr compile library.fsx --target:library
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
* Improvement of tree walking algorithm and support for caching
* nfsr config file to allow explicit control of files
* Less process wrapping (starting new fsi instances is expensive)
* Proxy files for consolidated dependencies
* Unix support - shell gateway script as opposed to batch file

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