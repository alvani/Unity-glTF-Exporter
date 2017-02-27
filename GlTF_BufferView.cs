using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class GlTF_BufferView : GlTF_Writer  {
	public string buffer;// ": "duck",
	public long byteLength;//": 25272,
	public long byteOffset;//": 0,
	public int target = 34962;
	//	public string target = "ARRAY_BUFFER";
	public int currentOffset = 0;
	public MemoryStream memoryStream = new MemoryStream();
	public bool bin = false;

	public GlTF_BufferView (string n) { name = n; }
	public GlTF_BufferView (string n, int t) { name = n; target = t; }

	public void Populate (int[] vs, bool flippedTriangle)
	{
		if (flippedTriangle)
		{
			for (int i = 0; i < vs.Length; i+=3)
			{
				ushort u = (ushort)vs[i];
				memoryStream.Write (BitConverter.GetBytes(u), 0, 2);
				currentOffset += 2;

				u = (ushort)vs[i+2];
				memoryStream.Write (BitConverter.GetBytes(u), 0, 2);
				currentOffset += 2;

				u = (ushort)vs[i+1];
				memoryStream.Write (BitConverter.GetBytes(u), 0, 2);
				currentOffset += 2;
			}
		}
		else
		{
			for (int i = 0; i < vs.Length; i++)
			{
				ushort u = (ushort)vs[i];
				memoryStream.Write (BitConverter.GetBytes(u), 0, 2);
				currentOffset += 2;
			}
		}
		byteLength = currentOffset;
	}

	public void Populate (float[] vs)
	{
		for (int i = 0; i < vs.Length; i++)
		{
			//			memoryStream.Write (vs[i]);
			//			memoryStream.Write ((byte[])vs, 0, vs.Length * sizeof(int));
			float f = vs[i];
			memoryStream.Write (BitConverter.GetBytes(f), 0, 4);
			currentOffset += 4;
		}
		byteLength = currentOffset;
	}

	public void Populate (float v)
	{
		memoryStream.Write (BitConverter.GetBytes(v), 0, 4);
		currentOffset += 4;
		byteLength = currentOffset;
	}

	public override void Write ()
	{
		/*
		"bufferView_4642": {
            "buffer": "vc.bin",
            "byteLength": 630080,
            "byteOffset": 0,
            "target": "ARRAY_BUFFER"
        },
	*/
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();
		var binName = binary ? "binary_glTF" : Path.GetFileNameWithoutExtension(GlTF_Writer.binFileName);
		CommaNL();
		Indent();		jsonWriter.Write ("\"buffer\": \"" + binName +"\"");
		CommaNL();
		Indent();		jsonWriter.Write ("\"byteLength\": " + byteLength);
		CommaNL();
		Indent();		jsonWriter.Write ("\"byteOffset\": " + byteOffset);
		if (target != -1) {
			CommaNL();
			Indent();		jsonWriter.Write ("\"target\": " + target);
		}
		jsonWriter.WriteLine();
		IndentOut();
		Indent();		jsonWriter.Write ("}");
	}
}
