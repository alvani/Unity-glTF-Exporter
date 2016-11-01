using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

// NOTES:
// - only checks first texture and first uv

public class TextureUnpacker {

	class Entry {
		public int left;
		public int right;
		public int top;
		public int bottom;
		public int texWidth;
		public int texHeight;
		public Dictionary<string, List<int>> subMeshMap = new Dictionary<string, List<int>>();
	}

	class UVTransform {
		public Vector2 offset;
		public Vector2 scale;
	}

	static Dictionary<string, Entry> entries = new Dictionary<string, Entry>(); // texture id->entry
	static Dictionary<string, Dictionary<int, UVTransform>> meshMap = new Dictionary<string, Dictionary<int, UVTransform>>();

	static Renderer GetRenderer(Transform tr) {
		Renderer mr = tr.GetComponent<MeshRenderer>();
		if (mr == null) {
			mr = tr.GetComponent<SkinnedMeshRenderer>();
		}
		return mr;
	}

	static Mesh GetMesh(Transform tr) {
		var mr = GetRenderer(tr);
		Mesh m = null;
		if (mr != null) {
			var t = mr.GetType();
			if (t == typeof(MeshRenderer)) {
				MeshFilter mf = tr.GetComponent<MeshFilter>();
				m = mf.sharedMesh;
			} else if (t == typeof(SkinnedMeshRenderer)) {
				SkinnedMeshRenderer smr = mr as SkinnedMeshRenderer;
				m = smr.sharedMesh;
			}
		}
		return m;
	}

	static List<Texture> GetTexturesFromRenderer(Renderer r) {
		var ret = new List<Texture>();

		var mats = r.sharedMaterials;
		foreach (var mat in mats) {
			var s = mat.shader;

			int spCount = ShaderUtil.GetPropertyCount(s);
			for (var i = 0; i < spCount; ++i) {
				var pName = ShaderUtil.GetPropertyName(s, i);
				var pType = ShaderUtil.GetPropertyType(s, i);

				if (pType == ShaderUtil.ShaderPropertyType.TexEnv) {
					var td = ShaderUtil.GetTexDim(s, i);
					if (td == ShaderUtil.ShaderPropertyTexDim.TexDim2D) {
						var t = mat.GetTexture(pName);
						if (t != null) {
							ret.Add(t);
						}
					}
				}
			}
		}

		return ret;
	}

	static List<Texture> GetTexturesFromMaterial(Material mat) {
		var ret = new List<Texture>();
		var s = mat.shader;

		int spCount = ShaderUtil.GetPropertyCount(s);
		for (var i = 0; i < spCount; ++i) {
			var pName = ShaderUtil.GetPropertyName(s, i);
			var pType = ShaderUtil.GetPropertyType(s, i);

			if (pType == ShaderUtil.ShaderPropertyType.TexEnv) {
				var td = ShaderUtil.GetTexDim(s, i);
				if (td == ShaderUtil.ShaderPropertyTexDim.TexDim2D) {
					var t = mat.GetTexture(pName);
					if (t != null) {
						ret.Add(t);
					}
				}
			}
		}

		return ret;
	}

	public static void CheckPackedTexture(Transform t) {
		var m = GetMesh(t);
		var r = GetRenderer(t);

		if (m != null && r != null) {
			var uvs = m.uv;
			for (int i = 0; i < m.subMeshCount; ++i) {
				Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
				Vector2 max = new Vector2(float.MinValue, float.MinValue);
				var tris = m.GetTriangles(i);
				for (int j = 0; j < tris.Length; ++j) {
					Vector2 uv = uvs[tris[j]];
					min.x = Mathf.Min(min.x, uv.x);
					min.y = Mathf.Min(min.y, uv.y);
					max.x = Mathf.Max(max.x, uv.x);
					max.y = Mathf.Max(max.y, uv.y);
				}

				var dx = max.x - min.x;
				var dy = max.y - min.y;

				if (r.sharedMaterials.Length > i && (dx < 0.9 || dy < 0.9)) {
					var mat = r.sharedMaterials[i];
					var texs = GetTexturesFromMaterial(mat);
					if (texs.Count > 0) {
						var tex = texs[0];
						var tw = tex.width;
						var th = tex.height;

						var sx = Mathf.FloorToInt(min.x * tw);
						var fx = Mathf.CeilToInt(max.x * tw);
						var sy = Mathf.FloorToInt(min.y * th);
						var fy = Mathf.CeilToInt(max.y * th);
						int wx = fx - sx;
						int wy = fy - sy;

						wx = Mathf.NextPowerOfTwo(wx);
						wy = Mathf.NextPowerOfTwo(wy);

						var meshName = GlTF_Mesh.GetNameFromObject(m);
						var name = GlTF_Texture.GetNameFromObject(tex);
						Entry e;
						if (entries.ContainsKey(name)) {
							e = entries[name];

							//merge
							var minX = Mathf.Min(e.left, sx);
							var maxX = Mathf.Max(e.right, fx);
							var minY = Mathf.Min(e.top, sy);
							var maxY = Mathf.Max(e.bottom, fy);

							var mw = maxX - minX;
							var mh = maxY - minY;

							mw = Mathf.NextPowerOfTwo(mw);
							mh = Mathf.NextPowerOfTwo(mh);

							e.left = minX;
							e.right = maxX;
							e.top = minY;
							e.bottom = maxY;
						} else {
							e = new Entry();
							e.left = sx;
							e.right = fx;
							e.top = sy;
							e.bottom = fy;
							e.texWidth = tex.width;
							e.texHeight = tex.height;
							entries[name] = e;
						}	

						List<int> subMeshId = null;
						if (e.subMeshMap.ContainsKey(meshName)) {
							subMeshId = e.subMeshMap[meshName];
						} else {
							subMeshId = new List<int>();
							e.subMeshMap[meshName] = subMeshId;
						}

						if (!subMeshId.Contains(i)) {
							subMeshId.Add(i);
						}
					}
				}
			}
		}			
	}

	public static void Reset() {
		entries.Clear();
		meshMap.Clear();
	}

	public static void Build() {
		foreach (var i in entries) {
			var e = i.Value;

			var mw = e.right - e.left;
			var mh = e.bottom - e.top;

			mw = Mathf.NextPowerOfTwo(mw);
			mh = Mathf.NextPowerOfTwo(mh);

			int left = e.left;
			int right = left + (mw - 1);
			if (right > e.texWidth - 1) {
				// shift left
				right = e.texWidth - 1;
				left = right - (mw - 1);
			}

			int top = e.top;
			int bottom = top + (mh - 1);
			if (bottom > e.texHeight - 1) {
				// shift up
				bottom = e.texHeight - 1;
				top = bottom - (mh - 1);
			}

			UVTransform uvt = new UVTransform();
			uvt.offset = new Vector2(-(float)left / (float)e.texWidth, -(float)top / (float)e.texHeight);
			uvt.scale = new Vector2((float)e.texWidth / (float)mw, (float)e.texHeight / (float)mh);

			foreach (var j in e.subMeshMap) {
				var list = j.Value;
				Dictionary<int, UVTransform> uvtMap = null;
				if (meshMap.ContainsKey(j.Key)) {
					uvtMap = meshMap[j.Key];
				} else {
					uvtMap = new Dictionary<int, UVTransform>();
					meshMap[j.Key] = uvtMap;
				}

				foreach (var subMeshIdx in list) {
					uvtMap[subMeshIdx] = uvt;	
				}
			}				
		}

		//debug
		foreach (var mesh in meshMap) {
			Debug.Log("mesh: " + mesh.Key);
			foreach (var uvt in mesh.Value) {
				Debug.Log("idx: " + uvt.Key);
			}
		}

	}

	public static void ProcessMesh(GlTF_Mesh mesh) {
//		mesh.name;
	}
}
