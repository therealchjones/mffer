# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# This configuration file also runs as a python module, and all variables in
# the resulting namespace are then accessible to sphinx when it is running.
# In addition to the variables/options defined by sphinx, we set:
# rootdir: the root workspace/project folder
# srcdir: topmost directory containing source code to be documented (src)
# docdir: topmost directory containing documentation (docs)
# configdir: directory containing this file (and possibly other config files)
# builddir:

# Note that this does *not* set the "root directory" that sphinx-build
# uses (which is set at the command line), but rather the directories
# described above.
from pathlib import Path
import os

if __file__ is None:
	raise Exception("Unable to determine the path of conf.py")
configdir = Path(__file__).parent
rootdir = configdir
while ( len(sorted(rootdir.glob('*.csproj'))) == 0 ) :
	parentdir = rootdir.parent
	if ( parentdir is None or parentdir.samefile(rootdir)) :
		raise Exception("Unable to find dotnet project file.")
	rootdir=parentdir
srcdir = next(rootdir.glob('src'))
docdir = next(rootdir.glob('docs'))
builddir = next(rootdir.glob('build'))
doxygendir = next(builddir.glob('doxygen'))
sphinxdir = next(builddir.glob('sphinx'))

# -- Path setup --------------------------------------------------------------

# If extensions (or modules to document with autodoc) are in another directory,
# add these directories to sys.path here. If the directory is relative to the
# documentation root, use os.path.abspath to make it absolute, like shown here.
#
# import os
# import sys
# sys.path.insert(0, os.path.abspath('.'))


# -- Project information -----------------------------------------------------

project = 'mffer'
copyright = 'No Rights Reserved'
author = 'Christian Jones'


# -- General configuration ---------------------------------------------------

# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions = [
	'myst_parser',
	'sphinx_rtd_theme',
	#'breathe',
	#'sphinx_csharp',
]

#breathe_projects = {
#	"api": doxygendir.as_posix(),
#}
#breathe_default_project = "api"

# Add any paths that contain templates here, relative to this directory.
templates_path = [
	os.path.join( docdir, '_templates'),
	]

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = [
	'_build',
	'Thumbs.db',
	'.DS_Store',
	'README.md',
	'**/README.md',
	'Doxyfile',
	'conf.py'
	]


# -- Options for HTML output -------------------------------------------------

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
#
html_theme = 'sphinx_rtd_theme'

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
html_static_path = []

if Path(os.path.join(docdir.as_posix(),'_static')).exists():
	html_static_path.append( os.path.join(docdir.as_posix(),'_static'))

html_extra_path = [
	doxygendir.as_posix(),
]

myst_heading_anchors = 4
