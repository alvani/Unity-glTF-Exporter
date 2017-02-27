using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlTF_Technique : GlTF_Writer {
	public enum Type {		 
		FLOAT = 5126,
		FLOAT_VEC2 = 35664,
		FLOAT_VEC3 = 35665,
		FLOAT_VEC4 = 35666,
		FLOAT_MAT3 = 35675,
		FLOAT_MAT4 = 35676,
		SAMPLER_2D = 35678
	}

	public enum Semantic {
		UNKNOWN,
		POSITION,
		NORMAL,
		TEXCOORD_0,
		TEXCOORD_1,
		TEXCOORD_2,
		TEXCOORD_3,
		MODELVIEW,
		PROJECTION,
		MODELVIEWINVERSETRANSPOSE,
		CESIUM_RTC_MODELVIEW,
		JOINT,
		WEIGHT,
		JOINTMATRIX
	}

	public class Parameter {	
		public string name;	
		public Type type;
		public Semantic semantic = Semantic.UNKNOWN;
		public int count = -1; // for joint matrix
	}

	public class Attribute {
		public string name;
		public string param;
	}

	public class Uniform {
		public string name;
		public string param;
	}

	public class States {
		public int[] enable;
		public Dictionary<string, Value> functions = new Dictionary<string, Value>();
	}		

	public class Value : GlTF_Writer {
		public enum Type {
			Unknown,
			Bool,
			Int,
			Float,
			Color,
			Vector2,
			Vector4,
			IntArr,
			BoolArr
		}

		bool boolValue;
		int intValue;
		float floatValue;
		Color colorValue;
		Vector2 vector2Value;
		Vector4 vector4Value;
		int[] intArrValue;
		bool[] boolArrvalue;
		Type type = Type.Unknown;

		public Value(bool value) 
		{
			boolValue = value;
			type = Type.Bool;
		}

		public Value(int value) 
		{
			intValue = value;
			type = Type.Int;
		}

		public Value(float value) 
		{
			floatValue = value;
			type = Type.Float;
		}

		public Value(Color value) 
		{
			colorValue = value;
			type = Type.Color;
		}

		public Value(Vector2 value) 
		{
			vector2Value = value;
			type = Type.Vector2;
		}

		public Value(Vector4 value) 
		{
			vector4Value = value;
			type = Type.Vector4;
		}

		public Value(int[] value)
		{
			intArrValue = value;
			type = Type.IntArr;
		}

		public Value(bool[] value)
		{
			boolArrvalue = value;
			type = Type.BoolArr;
		}

		void WriteArr<T>(T arr) where T:ArrayList
		{
			jsonWriter.Write("[");
			for (var i = 0; i < arr.Count; ++i)
			{
				jsonWriter.Write(arr[i].ToString().ToLower());
				if (i != arr.Count - 1)
				{
					jsonWriter.Write(", ");
				}
			}
			jsonWriter.Write("]");
		}

		public override void Write()
		{
			switch(type) 
			{
				case Type.Bool:
					jsonWriter.Write("[" + boolValue.ToString().ToLower() + "]");
				break;

				case Type.Int:
					jsonWriter.Write("[" + intValue + "]");
				break;

				case Type.Float:
					jsonWriter.Write("[" + floatValue + "]");
				break;

				case Type.Color:
					jsonWriter.Write("[" + colorValue.r + ", " + colorValue.g + ", " + colorValue.b + ", " + colorValue.a + "]");
				break;

				case Type.Vector2:
					jsonWriter.Write("[" + vector2Value.x + ", " + vector2Value.y + "]");
				break;

				case Type.Vector4:
					jsonWriter.Write("[" + vector4Value.x + ", " + vector4Value.y + ", " + vector4Value.z + ", " + vector4Value.w + "]");
				break;

				case Type.IntArr:
					WriteArr(new ArrayList(intArrValue));
				break;

			case Type.BoolArr:
					WriteArr(new ArrayList(boolArrvalue));
				break;
						
			}
		}
	}

	public string program;
	public List<Attribute> attributes = new List<Attribute>();
	public List<Parameter> parameters = new List<Parameter>();
	public List<Uniform> uniforms = new List<Uniform>();
	public States states = new States();

	public static string GetNameFromObject(Object o) 
	{		 		
		return "technique_" + GlTF_Writer.GetNameFromObject(o);
	}

	public void AddDefaultUniforms(bool rtc)
	{
		var tParam = new Parameter();
		tParam.name = "modelViewMatrix";
		tParam.type = Type.FLOAT_MAT4;
		tParam.semantic = rtc ? Semantic.CESIUM_RTC_MODELVIEW : Semantic.MODELVIEW;
		parameters.Add(tParam);
		var uni = new Uniform();
		uni.name = "u_modelViewMatrix";
		uni.param = tParam.name;
		uniforms.Add(uni);

		tParam = new Parameter();
		tParam.name = "projectionMatrix";
		tParam.type = Type.FLOAT_MAT4;
		tParam.semantic = Semantic.PROJECTION;
		parameters.Add(tParam);
		uni = new Uniform();
		uni.name = "u_projectionMatrix";
		uni.param = tParam.name;
		uniforms.Add(uni);

		tParam = new Parameter();
		tParam.name = "normalMatrix";
		tParam.type = Type.FLOAT_MAT3;
		tParam.semantic = Semantic.MODELVIEWINVERSETRANSPOSE;
		parameters.Add(tParam);
		uni = new Uniform();
		uni.name = "u_normalMatrix";
		uni.param = tParam.name;
		uniforms.Add(uni);
	}

	public override void Write()
	{
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();		
		Indent();		jsonWriter.Write ("\"program\": \"" + program +"\",\n");
		Indent();		jsonWriter.Write ("\"parameters\": {\n");
		IndentIn();
		foreach (var p in parameters)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"" + p.name + "\": {\n");
			IndentIn();

			CommaNL();
			Indent();	jsonWriter.Write ("\"type\": " + (int)p.type);
			if (p.semantic != Semantic.UNKNOWN)
			{
				CommaNL();
				Indent();	jsonWriter.Write ("\"semantic\": \"" + p.semantic + "\"");
			}
			if (p.count != -1)
			{
				CommaNL();
				Indent();	jsonWriter.Write("\"count\": " + p.count);
			}
			jsonWriter.WriteLine();

			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}
		Indent();		jsonWriter.Write ("\n");
		IndentOut();
		Indent();		jsonWriter.Write ("},\n");

		Indent();		jsonWriter.Write ("\"attributes\": {\n");
		IndentIn();
		foreach (var a in attributes)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"" + a.name + "\": \"" + a.param + "\"");
		}
		Indent();		jsonWriter.Write ("\n");
		IndentOut();
		Indent();		jsonWriter.Write ("},\n");

		Indent();		jsonWriter.Write ("\"uniforms\": {\n");
		IndentIn();
		foreach (var u in uniforms)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"" + u.name + "\": \"" + u.param + "\"");
		}
		Indent();		jsonWriter.Write ("\n");
		IndentOut();
		Indent();		jsonWriter.Write ("},\n");

		// states
		Indent();		jsonWriter.Write ("\"states\": {\n");
		IndentIn();

		if (states != null && states.enable != null)
		{
			Indent();	jsonWriter.Write("\"enable\": [\n");
			IndentIn();
			foreach (var en in states.enable)
			{
				CommaNL();
				Indent();	jsonWriter.Write(en);
			}
			jsonWriter.Write("\n");
			IndentOut();
			Indent();	jsonWriter.Write("]");
		}

		if (states != null && states.functions.Count > 0) 
		{
			jsonWriter.Write(",\n");
			Indent();	jsonWriter.Write("\"functions\": {\n");
			IndentIn();
			foreach (var fun in states.functions)
			{
				CommaNL();
				Indent();	jsonWriter.Write("\"" + fun.Key + "\": ");
				fun.Value.Write();
			}
			jsonWriter.Write("\n");
			IndentOut();
			Indent();	jsonWriter.Write("}");
			jsonWriter.Write("\n");
		} else {
			jsonWriter.Write("\n");
		}

		IndentOut();
		Indent();		jsonWriter.Write ("}\n");

		IndentOut();
		Indent();		jsonWriter.Write ("}");
	}
}
