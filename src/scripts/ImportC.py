from ghidra.app.util.cparser.CPP import PreProcessor
from ghidra.app.util.cparser.C import ParseException, CParser
from java.io import ByteArrayOutputStream, ByteArrayInputStream, FileOutputStream, PrintStream

args = getScriptArgs()
if args is None:
	headerFile = askFile("Select header file", "Choose File")
else:
	headerFile = args[0]

logFile = "/Users/chjones/Downloads/CParserPlugin.out"

cpp = PreProcessor()
cpp.setArgs( [ "-D_GHIDRA_" ] )
headerStream = ByteArrayOutputStream()
cpp.setOutputStream( headerStream )
cpp.parse(headerFile)

dtMgr = currentProgram.getDataTypeManager()
cpp.getDefinitions().populateDefineEquates(dtMgr)

cParser = CParser(dtMgr, True, None)
inputStream = ByteArrayInputStream(headerStream.toByteArray())
try:
	cParser.parse( inputStream )
except java.lang.Exception as e:
	print "ERROR:\n"
	print "Saving " + logFile
	fileOut = PrintStream(FileOutputStream(logFile))
	fileOut.println(headerStream)
	fileOut.close()
	print e.getMessage()
finally:
	headerStream.close()
	inputStream.close()
