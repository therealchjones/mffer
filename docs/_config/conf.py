# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# make 'srcdir' point to the proper place for the source file root,
# either in the current directory or in some parent directory
import os
srcdir = os.path.abspath('.')
while ( not os.path.exists( os.path.join(srcdir,'index.rst') ) ):
	newsrcdir = os.path.join(srcdir,'..')
	if ( os.path.samefile(srcdir,newsrcdir)):
		raise Exception("Unable to find index.rst")
	srcdir = newsrcdir

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
	'sphinx_rtd_theme'
]

# Add any paths that contain templates here, relative to this directory.
templates_path = [
	os.path.join( srcdir, '_templates'),
	]

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = ['_build', 'Thumbs.db', '.DS_Store', 'README.md']


# -- Options for HTML output -------------------------------------------------

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
#
html_theme = 'sphinx_rtd_theme'

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
html_static_path = [
	os.path.join(srcdir,'_static')
	]

myst_heading_anchors = 4
