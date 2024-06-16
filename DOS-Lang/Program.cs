using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualBasic;

class Program {

    struct HandleReturn {
        public string AsmReturn;
        public DataValue[] DataReturn;
        public int DataValueTop;
    }

    struct DataValue {
        public string identifier;
        public string value;
        public string dataType;
    }

    struct FunctionsUsed {
        public bool keyboard;
        public bool textMode;
        public bool VGA_graphics;
        public bool defined;
    }

    struct Function {
        public string identifier;
        public int parameters;
        public string[] parameterDataTypes;
    }

    struct parameter {
        public string identifier;
        public string dataType;
    }

    struct Variable {
        public string identifier;
        public int memoryAddress;
        public string dataType;
    }

    static void Main() {

        Console.Write("Enter the location of your source code: ");
        string SourceFileLocation = Console.ReadLine();

        if (SourceFileLocation == "" || SourceFileLocation == null) {Console.WriteLine("Empty source file location!"); return;} //escape if an invalid source file location is input

        StreamReader reader;
        try {
            reader = new(SourceFileLocation);
        }
        catch (FileNotFoundException) {
            Console.WriteLine("Source file not found!");
            return;
        }

        string[] SourceFile = LoadFile(reader);

        reader.Close();

        Console.Write("Enter the location of your desired output file: ");
        string? ObjectFileLocation = Console.ReadLine();
        if (ObjectFileLocation == "" || ObjectFileLocation == null) {Console.WriteLine("Empty object file location!"); return;} //escape if an invalid object file location is input

        int success = Build(SourceFile, "temp.asm", FileMode.Create);

        if (success == 1) {Console.WriteLine("Invalid command in source file"); return;}
        if (success == -1) {Console.WriteLine("Source file empty!"); return;}

        Directory.SetCurrentDirectory(".");
        Console.WriteLine(Directory.GetCurrentDirectory());
        Process assemble = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "sh", Arguments="./assemble", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            }
        };
        assemble.Start();
    }

    static HandleReturn HandlePrint(string lineIn, int dataValueTop) {
    
        HandleReturn print = new();
        bool inString = false;
        string currentString = "";
        List<DataValue> strings = [];
        string currentCmd;

        for (int i = lineIn.IndexOf('('); i < lineIn.Length; i++) {
            if (lineIn[i] == '"') {
                inString = !inString;
                if (!inString) {
                    strings.Add(BuildDataValue(currentString, dataValueTop, "string"));
                    currentString = "";
                    dataValueTop++;
                    
                }
            } else if (lineIn[i] == '/' && lineIn[i+1] == '/' & !inString) {
                i = lineIn.Length;
            } else if (lineIn[i] == '\\') {
                i++;
                if (lineIn[i] == 'n') {
                    currentString += "\", 10, \"";
                }
                
                if (!(i < lineIn.Length)) {
                    currentString += lineIn[i];
                }
            } else if (inString) {
                currentString += lineIn[i];
            }
        }

        print.DataReturn = [.. strings];
        print.DataValueTop = dataValueTop;
        foreach (DataValue str in print.DataReturn) {
            currentCmd = "mov si, " + str.identifier + "\ncall printText\n";
            print.AsmReturn += currentCmd;
        }

        return print;
    }

    static HandleReturn HandleWaitForKey(string lineIn, int dataValueTop) {

        HandleReturn waitForKey = new();
        bool inString = false;
        string currentValue = "";
        List<DataValue> dataSet = [];

        for (int i = lineIn.IndexOf('('); i < lineIn.Length; i++) {
            if (lineIn[i] == ',' || lineIn[i] == ')') {

                    dataSet.Add(BuildDataValue(currentValue, dataValueTop, "byte"));
                    currentValue = "";
                    dataValueTop++;

            } else if (lineIn[i] == '/' && lineIn[i+1] == '/' & !inString) {
                i = lineIn.Length;
            } else if (lineIn[i]!= '(') {
                currentValue += lineIn[i];
            }
        }

        waitForKey.DataReturn = [.. dataSet];
        waitForKey.DataValueTop = dataValueTop;
        waitForKey.AsmReturn = "mov bl,[" + waitForKey.DataReturn[0].identifier + "]\ncall loopCheckKey\n";

        return waitForKey;

    }

    static HandleReturn HandlePlotPixel(string lineIn, int dataValueTop) {
        
        HandleReturn plot = new();
        bool inString = false;
        string currentValue = "";
        List<DataValue> dataSet = [];

        for (int i = lineIn.IndexOf('('); i < lineIn.Length; i++) {
            if (lineIn[i] == ',' || lineIn[i] == ')') {

                    dataSet.Add(BuildDataValue(currentValue, dataValueTop, "word"));
                    currentValue = "";
                    dataValueTop++;

            } else if (lineIn[i] == '/' && lineIn[i+1] == '/' & !inString) {
                i = lineIn.Length;
            } else if (lineIn[i]!= '(' & lineIn[i]!= ' ') {
                currentValue += lineIn[i];
            }
        }

        plot.DataReturn = [.. dataSet];
        if (plot.DataReturn.Length != 3) {Console.WriteLine("Invalid use of plot pixel command!"); return new HandleReturn();}
        plot.AsmReturn = "mov bx," + plot.DataReturn[0].value + "\nmov dx," + plot.DataReturn[1].value + "\nmov cl,"+plot.DataReturn[2].value+"\ncall plotPixel\n";
        plot.DataReturn = [];
        plot.DataValueTop = dataValueTop;

        return plot;
    }

    static Function BuildFunction(string[] sourceContents, int si) {
        int parameterCount = 0;
        List<parameter> parameters = [];
        string temp = "";
        for (int i = sourceContents[si].IndexOf('('); i < sourceContents[si].Length; i++) {
            if (sourceContents[si][i] == ',') {
                parameters.Add(new() {
                    identifier = temp[(temp.IndexOf(' ') + 1)..],
                    dataType = temp[..temp.IndexOf(' ')]
                });
                parameterCount+=1;
            } else {
                temp += sourceContents[si][i];
            }
        }

        Function function = new() {identifier = sourceContents[si][(sourceContents[si].IndexOf(':')+1)..sourceContents[si].IndexOf('(')]};
        return function;
    }

    static DataValue BuildDataValue(string value, int identifierIndex, string type) {
        DataValue data = new()
        {
            value = value,
            identifier = "var" + identifierIndex,
            dataType = type
        };

        return data;
    }

    static int Build(string[] sourceContents, string fileName, FileMode fileMode) {
        StreamWriter TempAsmFile = new(File.Open(fileName, fileMode));
        TempAsmFile.Write("[ORG 0x1000]\njmp start\nstart:\n");
        HandleReturn functionRet = new();
        List<DataValue> dataAppend = [];
        List<Variable> variables = [];
        List<Function> definedFunctions = [];
        FunctionsUsed functionsUsed = new();
        int DataValueTop = 0;
        if (sourceContents == null) {return -1;}
        for (int i = 0; i < sourceContents.Length; i++) {
            Console.WriteLine(sourceContents[i]);
            if (sourceContents[i] == "") {} 
            else if (sourceContents[i].Contains(':') & sourceContents[i].Contains('(')) {
                switch (sourceContents[i][..sourceContents[i].IndexOf(':')]) {
                    case "def":
                        if (functionsUsed.defined == false) {
                            StreamWriter functionsWrite = new(File.Open("build/defined.asm", FileMode.Create));
                            functionsWrite.Close();
                            functionsUsed.defined = true;
                        }
                        definedFunctions.Add(BuildFunction(sourceContents, i));
                        break;
                }
            }
            else if (sourceContents[i].Contains('(')) {
                switch (sourceContents[i][..sourceContents[i].IndexOf('(')]) {

                    //Text Mode
                    case "print":
                        functionRet = HandlePrint(sourceContents[i], DataValueTop);
                        functionsUsed.textMode = true;
                        break;
                    case "enterTextMode":
                        functionRet = new() {
                            AsmReturn = "call initTextMode\n", DataReturn=[], DataValueTop=DataValueTop
                        };
                        functionsUsed.textMode = true;
                        break;

                    //Keyboard
                    case "waitForKey":
                        functionRet = HandleWaitForKey(sourceContents[i], DataValueTop);
                        functionsUsed.keyboard = true;
                        break;

                    //VGA
                    case "enterGraphicsMode":
                        functionRet = new() {
                            AsmReturn = "call initVGAMode\n", DataReturn=[], DataValueTop=DataValueTop
                        };
                        functionsUsed.VGA_graphics = true;
                        break;
                    case "plotPixel":
                        functionRet = HandlePlotPixel(sourceContents[i], DataValueTop);
                        if (functionRet.AsmReturn == "") {
                            return 1;
                        }
                        break;

                    default:
                        Console.WriteLine("Unknown command at line " + (i+1));
                        return 1;
                }
                
                DataValueTop = functionRet.DataValueTop;
                TempAsmFile.Write(functionRet.AsmReturn);
                foreach(DataValue data in functionRet.DataReturn) {
                    dataAppend.Add(data);
                }
            }
        }
        TempAsmFile.Write("\nret\n");
        if(functionsUsed.textMode){TempAsmFile.Write("%include \"BuildData/textdisplay.asm\"\n");}
        if(functionsUsed.keyboard){TempAsmFile.Write("%include \"BuildData/keyboard.asm\"\n");}
        if(functionsUsed.VGA_graphics){TempAsmFile.Write("%include \"BuildData/VGA.asm\"\n");}
        DataValue[] dataAppendConvert = [..dataAppend];
        foreach (DataValue data in dataAppendConvert) {
            if (data.dataType == "string"){TempAsmFile.Write(data.identifier+" db \""+data.value+"\",0\n");}
            else if (data.dataType == "byte" || data.dataType == "int" || data.dataType == "word") {TempAsmFile.Write(data.identifier + " db " + data.value + "\n");}
        }
        TempAsmFile.Write("times " + "512" + "-($-$$) db 0");
        TempAsmFile.Close();
        return 0;
    }

    static string[] LoadFile(StreamReader fileInput) {
        List<string> file = [];

        while (!fileInput.EndOfStream) { file.Add(fileInput.ReadLine());}

        return [.. file];
        
    }

    static int GetFileSize(StreamReader fileAccess) {

        int fileLength = 0;

        while (!fileAccess.EndOfStream) {
            fileAccess.ReadLine();
            fileLength++;
        }

        return fileLength;
    }
}