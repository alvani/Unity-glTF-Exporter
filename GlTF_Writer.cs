using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class GlTF_Writer {
	public static FileStream fs;
	public static StreamWriter jsonWriter;
	public static BinaryWriter binWriter;
	public static Stream binFile;
	public static int indent = 0;
	public static string binFileName;
	public static bool binary;
	public static bool b3dm;
	static bool[] firsts = new bool[100];
	public static GlTF_BufferView ushortBufferView = new GlTF_BufferView("ushortBufferView", 34963);
	public static GlTF_BufferView floatBufferView = new GlTF_BufferView("floatBufferView");
	public static GlTF_BufferView vec2BufferView = new GlTF_BufferView("vec2BufferView");
	public static GlTF_BufferView vec3BufferView = new GlTF_BufferView("vec3BufferView");
	public static GlTF_BufferView vec4BufferView = new GlTF_BufferView("vec4BufferView");
	public static GlTF_BufferView mat4BufferView = new GlTF_BufferView("mat4BufferView", -1);
	public static List<GlTF_BufferView> bufferViews = new List<GlTF_BufferView>();	
	public static List<GlTF_Camera> cameras = new List<GlTF_Camera>();
	public static List<GlTF_Light> lights = new List<GlTF_Light>();
	public static List<GlTF_Mesh> meshes = new List<GlTF_Mesh>();
	public static List<GlTF_Accessor> accessors = new List<GlTF_Accessor>();
	public static List<GlTF_Node> nodes = new List<GlTF_Node>();
	public static Dictionary<string, GlTF_Material> materials = new Dictionary<string, GlTF_Material>();
	public static Dictionary<string, GlTF_Sampler> samplers = new Dictionary<string, GlTF_Sampler>();
	public static Dictionary<string, GlTF_Texture> textures = new Dictionary<string, GlTF_Texture>();
	public static List<GlTF_Image> images = new List<GlTF_Image>();
	public static List<GlTF_Animation> animations = new List<GlTF_Animation>();
	public static List<GlTF_Skin> skins = new List<GlTF_Skin>();
	public static Dictionary<string, GlTF_Technique> techniques = new Dictionary<string, GlTF_Technique>();
	public static List<GlTF_Program> programs = new List<GlTF_Program>();
	public static List<GlTF_Shader> shaders = new List<GlTF_Shader>();

	const int B3DM_HEADER_SIZE = 24;

	public double[] RTCCenter;

	static public string GetNameFromObject(Object o, bool useId = false) 
	{
		var ret = o.name;
		ret = ret.Replace(" ", "_");
		ret = ret.Replace("/", "_");
		ret = ret.Replace("\\", "_");

		if (useId) 
		{
			ret += "_" + o.GetInstanceID();
		}
		return ret;
	}

	public void Init()
	{
		firsts = new bool[100];
		ushortBufferView = new GlTF_BufferView("ushortBufferView", 34963);
		floatBufferView = new GlTF_BufferView("floatBufferView");
		vec2BufferView = new GlTF_BufferView("vec2BufferView");
		vec3BufferView = new GlTF_BufferView("vec3BufferView");
		vec4BufferView = new GlTF_BufferView("vec4BufferView");
		bufferViews = new List<GlTF_BufferView>();	
		cameras = new List<GlTF_Camera>();
		lights = new List<GlTF_Light>();
		meshes = new List<GlTF_Mesh>();
		accessors = new List<GlTF_Accessor>();
		nodes = new List<GlTF_Node>();
		materials = new Dictionary<string, GlTF_Material>();
		samplers = new Dictionary<string, GlTF_Sampler>();
		textures = new Dictionary<string, GlTF_Texture>();
		images = new List<GlTF_Image>();
		animations = new List<GlTF_Animation>();
		skins = new List<GlTF_Skin>();
		techniques = new Dictionary<string, GlTF_Technique>();
		programs = new List<GlTF_Program>();
		shaders = new List<GlTF_Shader>();
	}

	public void Indent() {
		for (int i = 0; i < indent; i++)
			jsonWriter.Write ("\t");
	}

	public void IndentIn() {
		indent++;
		firsts[indent] = true;
	}

	public void IndentOut() {
		indent--;
	}

	public void CommaStart() {
		firsts[indent] = false;
	}

	public void CommaNL() {
		if (!firsts[indent])
			jsonWriter.Write (",\n");
		//		else
		//			jsonWriter.Write ("\n");
		firsts[indent] = false;
	}

	public string name; // name of this object

	public void OpenFiles (string filepath) {		
		fs = File.Open(filepath, FileMode.Create);

		if (binary)
		{
			binWriter = new BinaryWriter(fs);
			binFile = fs;

			long offset = 20 + (b3dm ? B3DM_HEADER_SIZE : 0);
			fs.Seek(offset, SeekOrigin.Begin); // header skip
		} 
		else 
		{
			// separate bin file
			binFileName = Path.GetFileNameWithoutExtension (filepath) + ".bin";
			var binPath = Path.Combine(Path.GetDirectoryName(filepath), binFileName);
			binFile = File.Open(binPath, FileMode.Create);
		}

		jsonWriter = new StreamWriter (fs);
	}

	public void CloseFiles() {
		if (binary)
		{
			binWriter.Close();
		}
		else 
		{
			binFile.Close();
		}

		jsonWriter.Close ();
		fs.Close();
	}

	public virtual void Write () {

		bufferViews.Add (ushortBufferView);
		bufferViews.Add (floatBufferView);
		bufferViews.Add (vec2BufferView);
		bufferViews.Add (vec3BufferView);
		bufferViews.Add (vec4BufferView);
		bufferViews.Add (mat4BufferView);

		ushortBufferView.bin = binary;
		floatBufferView.bin = binary;
		vec2BufferView.bin = binary;
		vec3BufferView.bin = binary;
		vec4BufferView.bin = binary;
		mat4BufferView.bin = binary;

		// write memory streams to binary file
		ushortBufferView.byteOffset = 0;
		ushortBufferView.Align(4);
		floatBufferView.byteOffset = ushortBufferView.byteLength;
		vec2BufferView.byteOffset = floatBufferView.byteOffset + floatBufferView.byteLength;
		vec3BufferView.byteOffset = vec2BufferView.byteOffset + vec2BufferView.byteLength;
		vec4BufferView.byteOffset = vec3BufferView.byteOffset + vec3BufferView.byteLength;
		mat4BufferView.byteOffset = vec4BufferView.byteOffset + vec4BufferView.byteLength;

		jsonWriter.Write ("{\n");
		IndentIn();

		// asset
		CommaNL();
		Indent();	jsonWriter.Write ("\"asset\": {\n");
		IndentIn();
		Indent();	jsonWriter.Write ("\"version\": \"1\"\n");
		IndentOut();
		Indent();	jsonWriter.Write ("}");

		if (!binary)
		{
			// FIX: Should support multiple buffers
			CommaNL();
			Indent();	jsonWriter.Write ("\"buffers\": {\n");
			IndentIn();
			Indent();	jsonWriter.Write ("\"" + Path.GetFileNameWithoutExtension(GlTF_Writer.binFileName) +"\": {\n");
			IndentIn();
			Indent();	jsonWriter.Write ("\"byteLength\": "+ (vec4BufferView.byteOffset+vec4BufferView.byteLength)+",\n");
			Indent();	jsonWriter.Write ("\"type\": \"arraybuffer\",\n");
			Indent();	jsonWriter.Write ("\"uri\": \"" + GlTF_Writer.binFileName + "\"\n");

			IndentOut();
			Indent();	jsonWriter.Write ("}\n");

			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}
		else
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"buffers\": {\n");
			IndentIn();
			Indent();	jsonWriter.Write ("\"binary_glTF\": {\n");
			IndentIn();
			Indent();	jsonWriter.Write ("\"byteLength\": "+ (vec4BufferView.byteOffset+vec4BufferView.byteLength)+",\n");
			Indent();	jsonWriter.Write ("\"type\": \"arraybuffer\"\n");

			IndentOut();
			Indent();	jsonWriter.Write ("}\n");

			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (cameras != null && cameras.Count > 0)
		{
			CommaNL();
			Indent();		jsonWriter.Write ("\"cameras\": {\n");
			IndentIn();
			foreach (GlTF_Camera c in cameras)
			{
				CommaNL();
				c.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();		jsonWriter.Write ("}");
		}

		if (accessors != null && accessors.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"accessors\": {\n");
			IndentIn();
			foreach (GlTF_Accessor a in accessors)
			{
				if (a.count > 0)
				{
					CommaNL();
					a.Write ();
				}
			}			
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (bufferViews != null && bufferViews.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"bufferViews\": {\n");
			IndentIn();
			foreach (GlTF_BufferView bv in bufferViews)
			{
				if (bv.byteLength > 0)
				{
					CommaNL();
					bv.Write ();
				}
			}			
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (meshes != null && meshes.Count > 0)
		{
			CommaNL();
			Indent();
			jsonWriter.Write ("\"meshes\": {\n");
			IndentIn();
			foreach (GlTF_Mesh m in meshes)
			{
				CommaNL();
				m.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();
			jsonWriter.Write ("}");
		}

		if (shaders != null && shaders.Count > 0) 
		{
			CommaNL();
			Indent();
			jsonWriter.Write ("\"shaders\": {\n");
			IndentIn();
			foreach (var s in shaders)
			{
				CommaNL();
				s.Write();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();
			jsonWriter.Write ("}");
		}

		if (programs != null && programs.Count > 0) 
		{
			CommaNL();
			Indent();
			jsonWriter.Write ("\"programs\": {\n");
			IndentIn();
			foreach (var p in programs)
			{
				CommaNL();
				p.Write();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();
			jsonWriter.Write ("}");
		}

		if (techniques != null && techniques.Count > 0) 
		{
			CommaNL();
			Indent();
			jsonWriter.Write ("\"techniques\": {\n");
			IndentIn();
			foreach (KeyValuePair<string, GlTF_Technique> k in techniques) 
			{
				CommaNL();
				k.Value.Write();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();
			jsonWriter.Write ("}");
		}

//		CommaNL();
//		string tqs = @"
//	'techniques': {
//		'technique1': {
//			'parameters': {
//				'ambient': {
//					'type': 35666
//				},
//				'diffuse': {
//					'type': 35678
//				},
//				'emission': {
//					'type': 35666
//				},
//				'light0Color': {
//					'type': 35665,
//					'value': [
//					    1,
//					    1,
//					    1
//					    ]
//				},
//				'light0Transform': {
//					'semantic': 'MODELVIEW',
//					'source': 'directionalLight1',
//					'type': 35676
//				},
//				'modelViewMatrix': {
//					'semantic': 'MODELVIEW',
//					'type': 35676
//				},
//				'normal': {
//					'semantic': 'NORMAL',
//					'type': 35665
//				},
//				'normalMatrix': {
//					'semantic': 'MODELVIEWINVERSETRANSPOSE',
//					'type': 35675
//				},
//				'position': {
//					'semantic': 'POSITION',
//					'type': 35665
//				},
//				'projectionMatrix': {
//					'semantic': 'PROJECTION',
//					'type': 35676
//				},
//				'shininess': {
//					'type': 5126
//				},
//				'specular': {
//					'type': 35666
//				},
//				'texcoord0': {
//					'semantic': 'TEXCOORD_0',
//					'type': 35664
//				}
//			},
//			'pass': 'defaultPass',
//			'passes': {
//				'defaultPass': {
//					'details': {
//						'commonProfile': {
//							'extras': {
//								'doubleSided': false
//							},
//							'lightingModel': 'Blinn',
//							'parameters': [
//							    'ambient',
//							    'diffuse',
//							    'emission',
//							    'light0Color',
//							    'light0Transform',
//							    'modelViewMatrix',
//							    'normalMatrix',
//							    'projectionMatrix',
//							    'shininess',
//							    'specular'
//							    ],
//							'texcoordBindings': {
//								'diffuse': 'TEXCOORD_0'
//							}
//						},
//						'type': 'COLLADA-1.4.1/commonProfile'
//					},
//					'instanceProgram': {
//						'attributes': {
//							'a_normal': 'normal',
//							'a_position': 'position',
//							'a_texcoord0': 'texcoord0'
//						},
//						'program': 'program_0',
//						'uniforms': {
//							'u_ambient': 'ambient',
//							'u_diffuse': 'diffuse',
//							'u_emission': 'emission',
//							'u_light0Color': 'light0Color',
//							'u_light0Transform': 'light0Transform',
//							'u_modelViewMatrix': 'modelViewMatrix',
//							'u_normalMatrix': 'normalMatrix',
//							'u_projectionMatrix': 'projectionMatrix',
//							'u_shininess': 'shininess',
//							'u_specular': 'specular'
//						}
//					},
//					'states': {
//						'enable': [
//						    2884,
//						    2929
//						    ]
//					}
//				}
//			}
//		}
//	}";
//		tqs = tqs.Replace ("'", "\"");
//		jsonWriter.Write (tqs);

		if (samplers.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"samplers\": {\n");
			IndentIn();
			foreach (KeyValuePair<string, GlTF_Sampler> s in samplers)
			{
				CommaNL();
				s.Value.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (textures.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"textures\": {\n");
			IndentIn();
			foreach (KeyValuePair<string,GlTF_Texture> t in textures)
			{
				CommaNL();
				t.Value.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (images.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"images\": {\n");
			IndentIn();
			foreach (var i in images)
			{
				CommaNL();
				i.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (materials.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"materials\": {\n");
			IndentIn();
			foreach (KeyValuePair<string,GlTF_Material> m in materials)
			{
				CommaNL();
				m.Value.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (animations.Count > 0)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"animations\": {\n");
			IndentIn();
			foreach (GlTF_Animation a in animations)
			{
				CommaNL();
				a.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}
		if (skins.Count > 0) 
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"skins\": {\n");
			IndentIn();
			foreach (GlTF_Skin s in skins)
			{
				CommaNL();
				s.Write ();
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();	jsonWriter.Write ("}");
		}

		if (nodes != null && nodes.Count > 0)
		{
			CommaNL();
			/*
		    "nodes": {
        "node-Alien": {
            "children": [],
            "matrix": [
*/
			Indent();			jsonWriter.Write ("\"nodes\": {\n");
			IndentIn();
			//			bool first = true;
			foreach (GlTF_Node n in nodes)
			{
				CommaNL();
				//				if (!first)
				//					jsonWriter.Write (",\n");
				n.Write ();
				//				first = false;
			}
			jsonWriter.WriteLine();
			IndentOut();
			Indent();			jsonWriter.Write ("}");

		}
		CommaNL();

		Indent();			jsonWriter.Write ("\"scene\": \"defaultScene\",\n");
		Indent();			jsonWriter.Write ("\"scenes\": {\n");
		IndentIn();
		Indent();			jsonWriter.Write ("\"defaultScene\": {\n");
		IndentIn();
		CommaNL();
		Indent();			jsonWriter.Write ("\"nodes\": [\n");
		IndentIn();
		foreach (GlTF_Node n in nodes)
		{
			if (!n.hasParent)
			{
				CommaNL();
				Indent();		jsonWriter.Write ("\"" + n.name + "\"");
			}
		}
		jsonWriter.WriteLine();
		IndentOut();
		Indent();			jsonWriter.Write ("]\n");
		IndentOut();
		Indent();			jsonWriter.Write ("}\n");
		IndentOut();
		Indent();			jsonWriter.Write ("}");

		List<string> extUsed = new List<string>();
		if (binary)
		{
			extUsed.Add("KHR_binary_glTF");
		}
		var rtc = RTCCenter != null && RTCCenter.Length == 3;
		if (rtc)
		{
			extUsed.Add("CESIUM_RTC");
		}			

		if (extUsed.Count > 0)
		{
			CommaNL();
			Indent();			jsonWriter.Write ("\"extensionsUsed\": [\n");
			IndentIn();

			for (var i = 0; i < extUsed.Count; ++i)
			{
				CommaNL();
				Indent();			jsonWriter.Write ("\"" + extUsed[i] + "\"");	
			}

			jsonWriter.Write ("\n");
			IndentOut();
			Indent();			jsonWriter.Write ("]");

		}			

		if (rtc) {
			CommaNL();
			Indent();			jsonWriter.Write ("\"extensions\": {\n");
			IndentIn();
			Indent();			jsonWriter.Write ("\"CESIUM_RTC\": {\n");
			IndentIn();
			Indent();			jsonWriter.Write ("\"center\": [\n");
			IndentIn();
			for (var i = 0; i < 3; ++i)
			{
				CommaNL();
				Indent();			jsonWriter.Write (RTCCenter[i]);
			}
			jsonWriter.Write ("\n");
			IndentOut();
			Indent();			jsonWriter.Write ("]\n");
			IndentOut();
			Indent();			jsonWriter.Write ("}\n");
			IndentOut();
			Indent();			jsonWriter.Write ("}");
		}

		jsonWriter.Write ("\n");
		IndentOut();
		Indent();			jsonWriter.Write ("}");

		jsonWriter.Flush();

		uint contentLength = 0;
		if (binary)
		{
			long curLen = fs.Position;
			var rem = curLen % 4;
			if (rem != 0)
			{
				// add padding if not aligned to 4 bytes
				var next = (curLen / 4 + 1) * 4;
				rem = next - curLen;
				for (int i = 0; i < rem; ++i)
				{
					jsonWriter.Write(" ");
				}
			}
			jsonWriter.Flush();

			// current pos - header size
			int offset = 20 + (b3dm ? B3DM_HEADER_SIZE : 0);
			contentLength = (uint)(fs.Position - offset);
		}
			

		ushortBufferView.memoryStream.WriteTo(binFile);
		floatBufferView.memoryStream.WriteTo(binFile);
		vec2BufferView.memoryStream.WriteTo (binFile);
		vec3BufferView.memoryStream.WriteTo (binFile);
		vec4BufferView.memoryStream.WriteTo (binFile);
		mat4BufferView.memoryStream.WriteTo(binFile);

		binFile.Flush();
		if (binary)
		{
			uint fileLength = (uint)fs.Length;

			// write header
			fs.Seek(0, SeekOrigin.Begin);

			if (b3dm) 
			{
				jsonWriter.Write("b3dm");	// magic
				jsonWriter.Flush();
				binWriter.Write(1);	// version
				binWriter.Write(fileLength);
				binWriter.Write(0);	// batchTableJSONByteLength
				binWriter.Write(0); // batchTableBinaryByteLength
				binWriter.Write(0); // batchLength
				binWriter.Flush();
			}

			jsonWriter.Write("glTF");	// magic
			jsonWriter.Flush();
			binWriter.Write(1);	// version
			uint l = (uint)(fileLength - (b3dm ? B3DM_HEADER_SIZE : 0)); // min b3dm header
			binWriter.Write(l);
			binWriter.Write(contentLength);
			binWriter.Write(0);	// format
			binWriter.Flush();
		}
	}
}
