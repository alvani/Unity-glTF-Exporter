using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class GlTF_Animation : GlTF_Writer {
	public List<GlTF_Channel> channels = new List<GlTF_Channel>();
	public GlTF_Parameters parameters;
	public List<GlTF_AnimSampler> animSamplers = new List<GlTF_AnimSampler>();
	bool gotTranslation = false;
	bool gotRotation = false;
	bool gotScale = false;

	class BoneAnimPath {
		public enum Type {
			Translation,
			Scale,
			Rotation
		}

		public GlTF_Target target;
		public GlTF_AnimSampler sampler;
		public GlTF_Channel channel;
		public GlTF_Accessor accessor;

		Type type;
		Vector3[] v3;
		Vector4[] v4;
		bool wx, wy, wz, ww;

		public BoneAnimPath(Type type, string animName, string objectName) {
			this.type = type;
			var typeStr = TypeToString(type);
			target = new GlTF_Target();
			target.id = objectName;
			target.path = typeStr;
			var subName = typeStr + "_" + animName + "_" + objectName;
			sampler = new GlTF_AnimSampler("sampler_" + subName, "param_" + subName);
			channel = new GlTF_Channel(sampler, target);
			accessor = new GlTF_Accessor("accessor_anim_" + subName, TypeToAccType(type), GlTF_Accessor.ComponentType.FLOAT);
			switch (type) {
				case Type.Translation: 
				case Type.Scale:
					accessor.bufferView = GlTF_Writer.vec3BufferView; 
					break;
				case Type.Rotation:
					accessor.bufferView = GlTF_Writer.vec4BufferView; 
					break;
			}
			GlTF_Writer.accessors.Add(accessor);
		}

		public void PopulateAccessor(AnimationClipCurveData cd, Keyframe[] refKeyFrames) {
			string propName = cd.propertyName;
			if (type == Type.Translation || type == Type.Scale) {
				if (v3 == null) {
					v3 = new Vector3[refKeyFrames.Length];
				}

				if (propName.Contains (".x")) {
					wx = true;
					for (var i = 0; i < refKeyFrames.Length; ++i) {
						v3[i].x = cd.curve.Evaluate(refKeyFrames[i].time);
					}
				} else if (propName.Contains (".y")) {
					wy = true;
					for (var i = 0; i < refKeyFrames.Length; ++i) {
						v3[i].y = cd.curve.Evaluate(refKeyFrames[i].time);
					}
				} else if (propName.Contains (".z")) {
					wz = true;
					for (var i = 0; i < refKeyFrames.Length; ++i) {
						v3[i].z = cd.curve.Evaluate(refKeyFrames[i].time);
					}
				}

				if (wx && wy && wz) {
					accessor.Populate(v3);
				}
			} else {
				if (v4 == null) {
					v4 = new Vector4[refKeyFrames.Length];
				}

				if (propName.Contains (".x")) {
					wx = true;
					for (var i = 0; i < refKeyFrames.Length; ++i) {
						v4[i].x = cd.curve.Evaluate(refKeyFrames[i].time);
					}
				} else if (propName.Contains (".y")) {
					wy = true;
					for (var i = 0; i < refKeyFrames.Length; ++i) {
						v4[i].y = cd.curve.Evaluate(refKeyFrames[i].time);
					}
				} else if (propName.Contains (".z")) {
					wz = true;
					for (var i = 0; i < refKeyFrames.Length; ++i) {
						v4[i].z = cd.curve.Evaluate(refKeyFrames[i].time);
					}
				} else if (propName.Contains (".w")) {
					ww = true;
					for (var i = 0; i < refKeyFrames.Length; ++i) {
						v4[i].w = cd.curve.Evaluate(refKeyFrames[i].time);
					}
				}

				if (wx && wy && wz && ww) {
					accessor.Populate(v4);
				}
			}				
		}

		string TypeToString(Type type) {
			switch (type) {
				case Type.Translation: return "translation";
				case Type.Scale: return "scale";
				case Type.Rotation: return "rotation";
			}
			return "";
		}

		GlTF_Accessor.Type TypeToAccType(Type type) {
			if (type == Type.Rotation) {
				return GlTF_Accessor.Type.VEC4;
			}
			return GlTF_Accessor.Type.VEC3;
		}
	}

	class BoneAnimData {		
		public BoneAnimPath position;
		public BoneAnimPath scale;
		public BoneAnimPath rotation;

		public BoneAnimData(string animName, string objectName) {			
			position = new BoneAnimPath(BoneAnimPath.Type.Translation, animName, objectName);
			scale = new BoneAnimPath(BoneAnimPath.Type.Scale, animName, objectName);
			rotation = new BoneAnimPath(BoneAnimPath.Type.Rotation, animName, objectName);
		}

		public void PopulateAccessor(AnimationClipCurveData cd, Keyframe[] refKeyFrames) {
			string propName = cd.propertyName;

			if (propName.Contains("m_LocalPosition")){
				position.PopulateAccessor(cd, refKeyFrames);
			} else if (propName.Contains("m_LocalRotation")){
				rotation.PopulateAccessor(cd, refKeyFrames);
			} else if (propName.Contains("m_LocalScale")){
				scale.PopulateAccessor(cd, refKeyFrames);
			}
		}
	}

	Dictionary<string, BoneAnimData> boneAnimData = new Dictionary<string, BoneAnimData>();
	GlTF_Accessor timeAccessor;

	public GlTF_Animation (string n) {
		name = n;
		parameters = new GlTF_Parameters(n);
	}

	public void Populate (AnimationClip c, Transform tr)
	{
		//	AnimationUtility.GetCurveBindings(c);
		// look at each curve
		// if position, rotation, scale detected for first time
		//  create channel, sampler, param for it
		//  populate this curve into proper component
		AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves(c, true);

		// Find curve which has most keyframes for time reference
		Keyframe[] refKeyFrames = null;
		for (int i = 0; i < curveDatas.Length; ++i) {
			var cd = curveDatas[i];
			if (refKeyFrames == null || cd.curve.keys.Length > refKeyFrames.Length) {
				refKeyFrames = cd.curve.keys;
			}
		}
		PopulateTime(c.name, refKeyFrames);

		for (int i = 0; i < curveDatas.Length; i++)
		{
			var cd = curveDatas[i];
			string propName = cd.propertyName;
			var boneTransform = GetTransformFromPath(cd.path, tr);
			if (boneTransform == null) {
				continue;
			}

			var boneName = GlTF_Node.GetNameFromObject(boneTransform);
			BoneAnimData bad;
			if (boneAnimData.ContainsKey(boneName)) {
				bad = boneAnimData[boneName];
			} else {
				bad = new BoneAnimData(c.name, boneName);
				boneAnimData[boneName] = bad;
			}
			bad.PopulateAccessor(cd, refKeyFrames);
		}
	}

	void PopulateTime(string animName, Keyframe[] keyFrames) {
		timeAccessor = new GlTF_Accessor("accessor_anim_time_" + animName, GlTF_Accessor.Type.SCALAR, GlTF_Accessor.ComponentType.FLOAT);
		timeAccessor.bufferView = GlTF_Writer.floatBufferView;
		GlTF_Writer.accessors.Add (timeAccessor);
		var times = new float[keyFrames.Length];
		for (int i = 0; i < keyFrames.Length; i++)
			times[i] = keyFrames[i].time;
		timeAccessor.Populate (times);
	}

	Transform GetTransformFromPath(string path, Transform tr) {		
		var name = path;
		var parent = tr;
		while (true) {
			var idx = name.IndexOf("/");
			if (idx != -1) {				
				var cn = name.Substring(0, idx);
				var c = parent.FindChild(cn);
				if (c != null) {
					parent = c;
				} else {
					// invalid path
					return null;
				}
				name = name.Substring(idx + 1);
			} else {
				parent = parent.FindChild(name);
				break;
			}
		}
		return parent;
	}

	public override void Write()
	{
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();
		Indent();		jsonWriter.Write ("\"channels\": [\n");
		foreach (BoneAnimData bad in boneAnimData.Values)
		{
			if (bad.position.accessor.count > 0)
			{
				CommaNL();
				bad.position.channel.Write();
			}
			if (bad.rotation.accessor.count > 0)
			{
				CommaNL();
				bad.rotation.channel.Write();
			}
			if (bad.scale.accessor.count > 0)
			{
				CommaNL();
				bad.scale.channel.Write();
			}
		}
		jsonWriter.WriteLine();
		Indent();		jsonWriter.Write ("]");
		CommaNL();

		Indent();		jsonWriter.Write ("\"parameters\": {\n");
		IndentIn();

		CommaNL();
		Indent();	jsonWriter.Write ("\"TIME\": \"" + timeAccessor.name + "\"");
		foreach (BoneAnimData bad in boneAnimData.Values)
		{
			if (bad.position.accessor.count > 0)
			{
				CommaNL();
				Indent();	jsonWriter.Write ("\"" + bad.position.sampler.output + "\": \"" + bad.position.accessor.name + "\"");
			}
			if (bad.rotation.accessor.count > 0)
			{
				CommaNL();
				Indent();	jsonWriter.Write ("\"" + bad.rotation.sampler.output + "\": \"" + bad.rotation.accessor.name + "\"");
			}
			if (bad.scale.accessor.count > 0)
			{
				CommaNL();
				Indent();	jsonWriter.Write ("\"" + bad.scale.sampler.output + "\": \"" + bad.scale.accessor.name + "\"");
			}
		}
		jsonWriter.WriteLine();

		IndentOut();
		Indent();		jsonWriter.Write ("}");
		CommaNL();

//		parameters.Write ();
//		CommaNL();

		Indent();		jsonWriter.Write ("\"samplers\": {\n");
		IndentIn();
		foreach (BoneAnimData bad in boneAnimData.Values)
		{
			if (bad.position.accessor.count > 0)
			{
				CommaNL();
				bad.position.sampler.Write();
			}
			if (bad.rotation.accessor.count > 0)
			{
				CommaNL();
				bad.rotation.sampler.Write();
			}
			if (bad.scale.accessor.count > 0)
			{
				CommaNL();
				bad.scale.sampler.Write();
			}
		}
		IndentOut();
		jsonWriter.WriteLine();
		Indent();		jsonWriter.Write ("}\n");

		IndentOut();
		Indent();		jsonWriter.Write ("}");
	}
}
