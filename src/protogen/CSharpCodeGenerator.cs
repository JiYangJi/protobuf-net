﻿using Google.Protobuf.Reflection;
using System.IO;
using System.Linq;

namespace ProtoBuf
{
    public class CSharpCodeGenerator : CommonCodeGenerator
    {
        public static CSharpCodeGenerator Default { get; } = new CSharpCodeGenerator();
        protected CSharpCodeGenerator() { }
        public override string Name => "C#";
        protected override string DefaultFileExtension => "cs";
        protected override string Escape(string identifier)
        {
            switch (identifier)
            {
                case "abstract":
                case "event":
                case "new":
                case "struct":
                case "as":
                case "explicit":
                case "null":
                case "switch":
                case "base":
                case "extern":
                case "object":
                case "this":
                case "bool":
                case "false":
                case "operator":
                case "throw":
                case "break":
                case "finally":
                case "out":
                case "true":
                case "byte":
                case "fixed":
                case "override":
                case "try":
                case "case":
                case "float":
                case "params":
                case "typeof":
                case "catch":
                case "for":
                case "private":
                case "uint":
                case "char":
                case "foreach":
                case "protected":
                case "ulong":
                case "checked":
                case "goto":
                case "public":
                case "unchecked":
                case "class":
                case "if":
                case "readonly":
                case "unsafe":
                case "const":
                case "implicit":
                case "ref":
                case "ushort":
                case "continue":
                case "in":
                case "return":
                case "using":
                case "decimal":
                case "int":
                case "sbyte":
                case "virtual":
                case "default":
                case "interface":
                case "sealed":
                case "volatile":
                case "delegate":
                case "internal":
                case "short":
                case "void":
                case "do":
                case "is":
                case "sizeof":
                case "while":
                case "double":
                case "lock":
                case "stackalloc":
                case "else":
                case "long":
                case "static":
                case "enum":
                case "namespace":
                case "string":
                    return "@" + identifier;
                default:
                    return identifier;
            }
        }

        protected override void WriteFileHeader(GeneratorContext ctx, FileDescriptorProto file, ref object state)
        {
            ctx.WriteLine("// This file was generated by a tool; you should avoid making direct changes.")
               .WriteLine("// Consider using 'partial classes' to extend these types")
               .WriteLine($"// Input: {Path.GetFileName(ctx.File.Name)}").WriteLine()
               .WriteLine("#pragma warning disable CS1591, CS0612, CS3021").WriteLine();


            var @namespace = file.Options?.CsharpNamespace ?? file.Package;

            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                state = @namespace;
                ctx.WriteLine($"namespace {@namespace}");
                ctx.WriteLine("{").Indent().WriteLine();
            }

        }
        protected override void WriteFileFooter(GeneratorContext ctx, FileDescriptorProto file, ref object state)
        {
            var @namespace = (string)state;
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                ctx.Outdent().WriteLine("}").WriteLine();
            }
            ctx.WriteLine("#pragma warning restore CS1591, CS0612, CS3021");
        }
        
        protected override void WriteEnumHeader(GeneratorContext ctx, EnumDescriptorProto obj, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            ctx.WriteLine($@"[global::ProtoBuf.ProtoContract(Name = @""{obj.Name}"")]");
            WriteOptions(ctx, obj.Options);
            ctx.WriteLine($"public enum {Escape(name)}").WriteLine("{").Indent();
        }

        protected override void WriteEnumFooter(GeneratorContext ctx, EnumDescriptorProto obj, ref object state)
        {
            ctx.Outdent().WriteLine("}").WriteLine();
        }

        protected override void WriteEnumValue(GeneratorContext ctx, EnumValueDescriptorProto obj, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            ctx.WriteLine($@"[global::ProtoBuf.ProtoEnum(Name = @""{obj.Name}"", Value = {obj.Number})]");
            WriteOptions(ctx, obj.Options);
            ctx.WriteLine($"{Escape(name)} = {obj.Number},");
        }

        protected override void WriteMessageFooter(GeneratorContext ctx, DescriptorProto obj, ref object state)
        {
            ctx.Outdent().WriteLine("}").WriteLine();
        }

        protected override void WriteMessageHeader(GeneratorContext ctx, DescriptorProto obj, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            ctx.WriteLine($@"[global::ProtoBuf.ProtoContract(Name = @""{obj.Name}"")]");
            WriteOptions(ctx, obj.Options);
            ctx.WriteLine($"public partial class {Escape(name)}");
            ctx.WriteLine("{").Indent();
        }

        private static void WriteOptions<T>(GeneratorContext ctx, T obj) where T : class, ISchemaOptions
        {
            if (obj == null) return;
            if (obj.Deprecated)
            {
                ctx.WriteLine($"[global::System.Obsolete]");
            }
        }

        const string FieldPrefix = "__pbn__";


        protected override void WriteField(GeneratorContext ctx, FieldDescriptorProto obj, ref object state, OneOfStub[] oneOfs)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            var tw = ctx.Write($@"[global::ProtoBuf.ProtoMember({obj.Number}, Name = @""{obj.Name}""");
            bool isOptional = obj.label == FieldDescriptorProto.Label.LabelOptional;
            bool isRepeated = obj.label == FieldDescriptorProto.Label.LabelRepeated;

            OneOfStub oneOf = obj.ShouldSerializeOneofIndex() ? oneOfs?[obj.OneofIndex] : null;
            if (oneOf != null && oneOf.CountTotal == 1)
            {
                oneOf = null; // not really a one-of, then!
            }
            bool explicitValues = isOptional && oneOf == null && ctx.Syntax == FileDescriptorProto.SyntaxProto2
                && obj.type != FieldDescriptorProto.Type.TypeMessage
                && obj.type != FieldDescriptorProto.Type.TypeGroup;


            string defaultValue = null;
            bool suppressDefaultAttribute = false;
            if (isOptional)
            {
                defaultValue = obj.DefaultValue;

                if (obj.type == FieldDescriptorProto.Type.TypeString)
                {
                    defaultValue = string.IsNullOrEmpty(defaultValue) ? "\"\""
                        : ("@\"" + (defaultValue ?? "").Replace("\"", "\"\"") + "\"");
                }
                else if (obj.type == FieldDescriptorProto.Type.TypeDouble)
                {
                    switch (defaultValue)
                    {
                        case "inf": defaultValue = "double.PositiveInfinity"; break;
                        case "-inf": defaultValue = "double.NegativeInfinity"; break;
                        case "nan": defaultValue = "double.NaN"; break;
                    }
                }
                else if (obj.type == FieldDescriptorProto.Type.TypeFloat)
                {
                    switch (defaultValue)
                    {
                        case "inf": defaultValue = "float.PositiveInfinity"; break;
                        case "-inf": defaultValue = "float.NegativeInfinity"; break;
                        case "nan": defaultValue = "float.NaN"; break;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(defaultValue) && obj.type == FieldDescriptorProto.Type.TypeEnum)
                {
                    var enumType = ctx.TryFind<EnumDescriptorProto>(obj.TypeName);
                    if (enumType != null)
                    {
                        var found = enumType.Values.FirstOrDefault(x => x.Name == defaultValue);
                        if (found != null) defaultValue = ctx.NameNormalizer.GetName(found);
                        defaultValue = ctx.NameNormalizer.GetName(enumType) + "." + defaultValue;
                    }
                }
            }
            var typeName = GetTypeName(ctx, obj, out var dataFormat, out var isMap);
            if (!string.IsNullOrWhiteSpace(dataFormat))
            {
                tw.Write($", DataFormat = global::ProtoBuf.DataFormat.{dataFormat}");
            }
            if (obj.Options?.Packed ?? false)
            {
                tw.Write($", IsPacked = true");
            }
            if (obj.label == FieldDescriptorProto.Label.LabelRequired)
            {
                tw.Write($", IsRequired = true");
            }
            tw.WriteLine(")]");
            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue) && !suppressDefaultAttribute)
            {
                ctx.WriteLine($"[global::System.ComponentModel.DefaultValue({defaultValue})]");
            }
            WriteOptions(ctx, obj.Options);
            if (isRepeated)
            {
                var mapMsgType = isMap ? ctx.TryFind<DescriptorProto>(obj.TypeName) : null;
                if (mapMsgType != null)
                {
                    var keyTypeName = GetTypeName(ctx, mapMsgType.Fields.Single(x => x.Number == 1),
                        out var keyDataFormat, out var _);
                    var valueTypeName = GetTypeName(ctx, mapMsgType.Fields.Single(x => x.Number == 2),
                        out var valueDataFormat, out var _);

                    bool first = true;
                    tw = ctx.Write($"[global::ProtoBuf.ProtoMap");
                    if (!string.IsNullOrWhiteSpace(keyDataFormat))
                    {
                        tw.Write($"{(first ? "(" : ", ")}KeyFormat = global::ProtoBuf.DataFormat.{keyDataFormat}");
                        first = false;
                    }
                    if (!string.IsNullOrWhiteSpace(valueDataFormat))
                    {
                        tw.Write($"{(first ? "(" : ", ")}ValueFormat = global::ProtoBuf.DataFormat.{valueDataFormat}");
                        first = false;
                    }
                    tw.WriteLine(first ? "]" : ")]");
                    ctx.WriteLine($"public global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}> {Escape(name)} {{ get; }} = new global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}>();");
                }
                else if (UseArray(obj))
                {
                    ctx.WriteLine($"public {typeName}[] {Escape(name)} {{ get; set; }}");
                }
                else
                {
                    ctx.WriteLine($"public global::System.Collections.Generic.List<{typeName}> {Escape(name)} {{ get; }} = new global::System.Collections.Generic.List<{typeName}>();");
                }
            }
            else if (oneOf != null)
            {
                var defValue = string.IsNullOrWhiteSpace(defaultValue) ? $"default({typeName})" : defaultValue;
                var fieldName = FieldPrefix + oneOf.OneOf.Name;
                var storage = oneOf.GetStorage(obj.type);
                ctx.WriteLine($"public {typeName} {Escape(name)}").WriteLine("{").Indent();

                switch (obj.type)
                {
                    case FieldDescriptorProto.Type.TypeMessage:
                    case FieldDescriptorProto.Type.TypeGroup:
                    case FieldDescriptorProto.Type.TypeEnum:
                    case FieldDescriptorProto.Type.TypeBytes:
                    case FieldDescriptorProto.Type.TypeString:
                        ctx.WriteLine($"get {{ return {fieldName}.Is({obj.Number}) ? (({typeName}){fieldName}.{storage}) : {defValue}; }}");
                        break;
                    default:
                        ctx.WriteLine($"get {{ return {fieldName}.Is({obj.Number}) ? {fieldName}.{storage} : {defValue}; }}");
                        break;
                }
                var unionType = oneOf.GetUnionType();
                ctx.WriteLine($"set {{ {fieldName} = new global::ProtoBuf.{unionType}({obj.Number}, value); }}")
                    .Outdent().WriteLine("}")
                    .WriteLine($"public bool ShouldSerialize{name}() => {fieldName}.Is({obj.Number});")
                    .WriteLine($"public void Reset{name}() => global::ProtoBuf.{unionType}.Reset(ref {fieldName}, {obj.Number});");

                if (oneOf.IsFirst())
                {
                    ctx.WriteLine().WriteLine($"private global::ProtoBuf.{unionType} {fieldName};");
                }
            }
            else if (explicitValues)
            {
                string fieldName = FieldPrefix + name, fieldType;
                bool isRef = false;
                switch (obj.type)
                {
                    case FieldDescriptorProto.Type.TypeString:
                    case FieldDescriptorProto.Type.TypeBytes:
                        fieldType = typeName;
                        isRef = true;
                        break;
                    default:
                        fieldType = typeName + "?";
                        break;
                }
                ctx.WriteLine($"public {typeName} {Escape(name)}").WriteLine("{").Indent();
                tw = ctx.Write($"get {{ return {fieldName}");
                if (!string.IsNullOrWhiteSpace(defaultValue))
                {
                    tw.Write(" ?? ");
                    tw.Write(defaultValue);
                }
                else if (!isRef)
                {
                    tw.Write(".GetValueOrDefault()");
                }
                tw.WriteLine("; }");
                ctx.WriteLine($"set {{ {fieldName} = value; }}")
                    .Outdent().WriteLine("}")
                    .WriteLine($"public bool ShouldSerialize{name}() => {fieldName} != null;")
                    .WriteLine($"public void Reset{name}() => {fieldName} = null;")
                    .WriteLine($"private {fieldType} {fieldName};");
            }
            else
            {
                tw = ctx.Write($"public {typeName} {Escape(name)} {{ get; set; }}");
                if (!string.IsNullOrWhiteSpace(defaultValue)) tw.Write($" = {defaultValue};");
                tw.WriteLine();
            }
            ctx.WriteLine();
        }
        private static bool UseArray(FieldDescriptorProto field)
        {
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TypeBool:
                case FieldDescriptorProto.Type.TypeDouble:
                case FieldDescriptorProto.Type.TypeFixed32:
                case FieldDescriptorProto.Type.TypeFixed64:
                case FieldDescriptorProto.Type.TypeFloat:
                case FieldDescriptorProto.Type.TypeInt32:
                case FieldDescriptorProto.Type.TypeInt64:
                case FieldDescriptorProto.Type.TypeSfixed32:
                case FieldDescriptorProto.Type.TypeSfixed64:
                case FieldDescriptorProto.Type.TypeSint32:
                case FieldDescriptorProto.Type.TypeSint64:
                case FieldDescriptorProto.Type.TypeUint32:
                case FieldDescriptorProto.Type.TypeUint64:
                    return true;
                default:
                    return false;
            }
        }

        private string GetTypeName(GeneratorContext ctx, FieldDescriptorProto field, out string dataFormat, out bool isMap)
        {
            dataFormat = "";
            isMap = false;
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TypeDouble:
                    return "double";
                case FieldDescriptorProto.Type.TypeFloat:
                    return "float";
                case FieldDescriptorProto.Type.TypeBool:
                    return "bool";
                case FieldDescriptorProto.Type.TypeString:
                    return "string";
                case FieldDescriptorProto.Type.TypeSint32:
                    dataFormat = nameof(DataFormat.ZigZag);
                    return "int";
                case FieldDescriptorProto.Type.TypeInt32:
                    return "int";
                case FieldDescriptorProto.Type.TypeSfixed32:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "int";
                case FieldDescriptorProto.Type.TypeSint64:
                    dataFormat = nameof(DataFormat.ZigZag);
                    return "long";
                case FieldDescriptorProto.Type.TypeInt64:
                    return "long";
                case FieldDescriptorProto.Type.TypeSfixed64:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "long";
                case FieldDescriptorProto.Type.TypeFixed32:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "uint";
                case FieldDescriptorProto.Type.TypeUint32:
                    return "uint";
                case FieldDescriptorProto.Type.TypeFixed64:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "ulong";
                case FieldDescriptorProto.Type.TypeUint64:
                    return "ulong";
                case FieldDescriptorProto.Type.TypeBytes:
                    return "byte[]";
                case FieldDescriptorProto.Type.TypeEnum:
                    var enumType = ctx.TryFind<EnumDescriptorProto>(field.TypeName);
                    return enumType == null ? field.TypeName : ctx.NameNormalizer.GetName(enumType);
                case FieldDescriptorProto.Type.TypeGroup:
                case FieldDescriptorProto.Type.TypeMessage:
                    switch(field.TypeName)
                    {
                        case WellKnownTypeTimestamp:
                            dataFormat = nameof(DataFormat.WellKnown);
                            return "global::System.DateTime?";
                        case WellKnownTypeDuration:
                            dataFormat = nameof(DataFormat.WellKnown);
                            return "global::System.TimeSpan?";
                    }
                    var msgType = ctx.TryFind<DescriptorProto>(field.TypeName);
                    if (field.type == FieldDescriptorProto.Type.TypeGroup)
                    {
                        dataFormat = nameof(DataFormat.Group);
                    }
                    isMap = msgType?.Options?.MapEntry ?? false;
                    return msgType == null ? field.TypeName : ctx.NameNormalizer.GetName(msgType);
                default:
                    return field.TypeName;
            }
        }

        const string WellKnownTypeTimestamp = ".google.protobuf.Timestamp",
                     WellKnownTypeDuration = ".google.protobuf.Duration";
    }
}
