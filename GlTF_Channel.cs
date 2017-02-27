using UnityEngine;
using System.Collections;

public class GlTF_Channel : GlTF_Writer {
	public GlTF_AnimSampler sampler;
	public GlTF_Target target;

	public GlTF_Channel (GlTF_AnimSampler s, GlTF_Target t) {
		sampler = s;
		target = t;
	}

	public override void Write()
	{
		IndentIn();
		Indent();		jsonWriter.Write ("{\n");
		IndentIn();
		Indent();		jsonWriter.Write ("\"sampler\": \"" + sampler.name + "\",\n");
		target.Write ();
		jsonWriter.WriteLine();
		IndentOut();
		Indent();		jsonWriter.Write ("}");
		IndentOut();
	}
}
