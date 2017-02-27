using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlTF_Attributes : GlTF_Writer {
	public GlTF_Accessor normalAccessor;
	public GlTF_Accessor positionAccessor;
	public GlTF_Accessor texCoord0Accessor;
	public GlTF_Accessor texCoord1Accessor;
	public GlTF_Accessor texCoord2Accessor;
	public GlTF_Accessor texCoord3Accessor;
	public GlTF_Accessor boneIndexAccessor;
	public GlTF_Accessor boneWeightAccessor;

	public void Populate (Mesh m)
	{
		positionAccessor.Populate (m.vertices);
		if (normalAccessor != null) 
		{
			normalAccessor.Populate (m.normals);
		}
		if (texCoord0Accessor != null) 
		{
			texCoord0Accessor.Populate (m.uv, true);
		}
		if (texCoord1Accessor != null)
		{
			texCoord1Accessor.Populate (m.uv2, true);
		}
		if (texCoord2Accessor != null) 
		{
			texCoord2Accessor.Populate (m.uv3, true);
		}
		if (texCoord3Accessor != null) 
		{
			texCoord3Accessor.Populate (m.uv4, true);
		}
		if (boneIndexAccessor != null)
		{
			List<Vector4> indices = new List<Vector4>();
			foreach (var bw in m.boneWeights) 
			{
				indices.Add(new Vector4(bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3));
			}
			boneIndexAccessor.Populate(indices.ToArray());
		}
		if (boneWeightAccessor != null)
		{
			List<Vector4> weights = new List<Vector4>();
			foreach (var bw in m.boneWeights) 
			{
				weights.Add(new Vector4(bw.weight0, bw.weight1, bw.weight2, bw.weight3));
			}
			boneWeightAccessor.Populate(weights.ToArray());
		}
	}

	public override void Write ()
	{
		Indent();	jsonWriter.Write ("\"attributes\": {\n");
		IndentIn();
		if (positionAccessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"POSITION\": \"" + positionAccessor.name + "\"");
		}
		if (normalAccessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"NORMAL\": \"" + normalAccessor.name + "\"");
		}
		if (texCoord0Accessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"TEXCOORD_0\": \"" + texCoord0Accessor.name + "\"");
		}
		if (texCoord1Accessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"TEXCOORD_1\": \"" + texCoord1Accessor.name + "\"");
		}
		if (texCoord2Accessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"TEXCOORD_2\": \"" + texCoord2Accessor.name + "\"");
		}
		if (texCoord3Accessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"TEXCOORD_3\": \"" + texCoord3Accessor.name + "\"");
		}
		if (boneIndexAccessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"JOINT\": \"" + boneIndexAccessor.name + "\"");
		}
		if (boneWeightAccessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"WEIGHT\": \"" + boneWeightAccessor.name + "\"");
		}
		//CommaNL();
		jsonWriter.WriteLine();
		IndentOut();
		Indent();	jsonWriter.Write ("}");
	}

}
