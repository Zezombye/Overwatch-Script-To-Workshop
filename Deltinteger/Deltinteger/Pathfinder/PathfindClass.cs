using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathmapClass : CodeType
    {
        public PathmapClass() : base("Pathmap")
        {
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this)
            };
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Create the class.
            var objectData = actionSet.Translate.DeltinScript.SetupClasses().CreateObject(actionSet, "_new_PathMap");

            // Get the pathmap data.
            PathMap pathMap = (PathMap)additionalParameterData[0];

            actionSet.AddAction(objectData.ClassObject.SetVariable(pathMap.NodesAsWorkshopData(), null, 0));
            actionSet.AddAction(objectData.ClassObject.SetVariable(pathMap.SegmentsAsWorkshopData(), null, 1));

            return objectData.ClassReference.GetVariable();
        }

        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = "Pathmap",
            Kind = CompletionItemKind.Class
        };
    }

    class PathmapClassConstructor : Constructor
    {
        public PathmapClassConstructor(PathmapClass pathMapClass) : base(pathMapClass, null, AccessLevel.Public)
        {
            Parameters = new CodeParameter[] {
                new PathmapFileParameter("pathmapFile", "File path of the pathmap to use.")
            };
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues) => throw new NotImplementedException();
    }

    class FileParameter : CodeParameter
    {
        public string FileType { get; }

        /// <summary>
        /// A parameter that resolves to a file. AdditionalParameterData will return the file path.
        /// </summary>
        /// <param name="fileType">The expected file type. Can be null.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="description">The parameter's description. Can be null.</param>
        public FileParameter(string fileType, string parameterName, string description) : base(parameterName, null, null, description)
        {
            FileType = fileType?.ToLower();
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            StringAction str = value as StringAction;
            if (str == null)
            {
                script.Diagnostics.Error("Expected string constant.", valueRange);
                return null;
            }

            string resultingPath = Extras.CombinePathWithDotNotation(script.Uri.FilePath(), str.Value);
            
            if (resultingPath == null)
            {
                script.Diagnostics.Error("File path contains invalid characters.", valueRange);
                return null;
            }

            string dir = Path.GetDirectoryName(resultingPath);
            if (Directory.Exists(dir))
                DeltinScript.AddImportCompletion(script, dir, valueRange);

            if (!File.Exists(resultingPath))
            {
                script.Diagnostics.Error($"No file was found at '{resultingPath}'.", valueRange);
                return null;
            }

            if (FileType != null && Path.GetExtension(resultingPath).ToLower() != "." + FileType)
            {
                script.Diagnostics.Error($"Expected a file with the file type '{FileType}'.", valueRange);
                return null;
            }

            return resultingPath;
        }
    }

    class PathmapFileParameter : FileParameter
    {
        public PathmapFileParameter(string file, string description) : base("pathmap", file, description)
        {
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            string filepath = base.Validate(script, value, valueRange) as string;
            if (filepath == null) return null;

            PathMap map;
            try
            {
                map = PathMap.ImportFromXML(filepath);
            }
            catch (InvalidOperationException)
            {
                script.Diagnostics.Error("Failed to deserialize the PathMap.", valueRange);
                return null;
            }

            return map;
        }
    }
}