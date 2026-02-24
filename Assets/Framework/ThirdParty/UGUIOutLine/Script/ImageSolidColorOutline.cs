/*
build by AppsTools.net
*/
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AppsTools
{
    [AddComponentMenu("UI/AppsTools/Image Solid Color Outline"), RequireComponent(typeof(Graphic))]
    public class ImageSolidColorOutline : BaseMeshEffect
    {
        public Color OutlineColor = Color.white;
        [Range(0, 15)]
        public int OutlineWidth = 0;

        private Color inilColor;
        private int inilOutlineWidth;

        private static List<UIVertex> m_VetexList = new List<UIVertex>();

        protected void StartInit()
        {
            var shader = Shader.Find("AppsTools.net/ImageOutline");
            base.graphic.material = new Material(shader);

            var v1 = base.graphic.canvas.additionalShaderChannels;
            var v2 = AdditionalCanvasShaderChannels.TexCoord1;
            if ((v1 & v2) != v2)
            {
                base.graphic.canvas.additionalShaderChannels |= v2;
            }
            v2 = AdditionalCanvasShaderChannels.TexCoord2;
            if ((v1 & v2) != v2)
            {
                base.graphic.canvas.additionalShaderChannels |= v2;
            }

            this._Refresh();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            Update();
        }
#endif


        /// <summary>
        /// The status of Outline will be refreshed.
        /// </summary>
        public void Update()
        {
            if (inilColor != OutlineColor || inilOutlineWidth != OutlineWidth)
            {
                inilColor = OutlineColor;
                inilOutlineWidth = OutlineWidth;
                if (base.graphic.material != null)
                {
                    this._Refresh();
                }
            }
        }

        private void _Refresh()
        {
            base.graphic.material.SetColor("_OutlineColor", this.OutlineColor);
            base.graphic.material.SetInt("_OutlineWidth", this.OutlineWidth);
            base.graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            vh.GetUIVertexStream(m_VetexList);

            this._ProcessVertices();

            vh.Clear();
            vh.AddUIVertexTriangleStream(m_VetexList);
        }

        private void _ProcessVertices()
        {
            for (int i = 0, count = m_VetexList.Count - 3; i <= count; i += 3)
            {
                var v1 = m_VetexList[i];
                var v2 = m_VetexList[i + 1];
                var v3 = m_VetexList[i + 2];
 
                var minX = _Min(v1.position.x, v2.position.x, v3.position.x);
                var minY = _Min(v1.position.y, v2.position.y, v3.position.y);
                var maxX = _Max(v1.position.x, v2.position.x, v3.position.x);
                var maxY = _Max(v1.position.y, v2.position.y, v3.position.y);
                var posCenter = new Vector2(minX + maxX, minY + maxY) * 0.5f;
           
                Vector2 triX, triY, uvX, uvY;
                Vector2 pos1 = v1.position;
                Vector2 pos2 = v2.position;
                Vector2 pos3 = v3.position;
                if (Mathf.Abs(Vector2.Dot((pos2 - pos1).normalized, Vector2.right))
                    > Mathf.Abs(Vector2.Dot((pos3 - pos2).normalized, Vector2.right)))
                {
                    triX = pos2 - pos1;
                    triY = pos3 - pos2;
                    uvX = v2.uv0 - v1.uv0;
                    uvY = v3.uv0 - v2.uv0;
                }
                else
                {
                    triX = pos3 - pos2;
                    triY = pos2 - pos1;
                    uvX = v3.uv0 - v2.uv0;
                    uvY = v2.uv0 - v1.uv0;
                }
              
                var uvMin = _Min(v1.uv0, v2.uv0, v3.uv0);
                var uvMax = _Max(v1.uv0, v2.uv0, v3.uv0);
                var uvOrigin = new Vector4(uvMin.x, uvMin.y, uvMax.x, uvMax.y);
                
                v1 = _SetNewPosAndUV(v1, this.OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
                v2 = _SetNewPosAndUV(v2, this.OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
                v3 = _SetNewPosAndUV(v3, this.OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
              
                m_VetexList[i] = v1;
                m_VetexList[i + 1] = v2;
                m_VetexList[i + 2] = v3;
            }
        }

        private static UIVertex _SetNewPosAndUV(UIVertex pVertex, int pOutLineWidth,
            Vector2 pPosCenter,
            Vector2 pTriangleX, Vector2 pTriangleY,
            Vector2 pUVX, Vector2 pUVY,
            Vector4 pUVOrigin)
        {
            // Position
            var pos = pVertex.position;
            var posXOffset = pos.x > pPosCenter.x ? pOutLineWidth : -pOutLineWidth;
            var posYOffset = pos.y > pPosCenter.y ? pOutLineWidth : -pOutLineWidth;
            pos.x += posXOffset;
            pos.y += posYOffset;
            pVertex.position = pos;
            // UV
            var uv = pVertex.uv0;
            var uv1 = pUVX / pTriangleX.magnitude * posXOffset * (Vector2.Dot(pTriangleX, Vector2.right) > 0 ? 1 : -1);
          
            var uv2 = pUVY / pTriangleY.magnitude * posYOffset * (Vector2.Dot(pTriangleY, Vector2.up) > 0 ? 1 : -1);
            uv.x += uv1.x;
            uv.y += uv1.y;
            uv.x += uv2.x;
            uv.y += uv2.y;
            //uv += new Vector4(uv1.x, uv1.y, 0, 0);
            //uv += new Vector4(uv2.x, uv2.y, 0, 0);
            pVertex.uv0 = uv;


            pVertex.uv1 = new Vector2(pUVOrigin.x, pUVOrigin.y);
            pVertex.uv2 = new Vector2(pUVOrigin.z, pUVOrigin.w);

            return pVertex;
        }

        private static float _Min(float pA, float pB, float pC)
        {
            return Mathf.Min(Mathf.Min(pA, pB), pC);
        }

        private static float _Max(float pA, float pB, float pC)
        {
            return Mathf.Max(Mathf.Max(pA, pB), pC);
        }

        private static Vector2 _Min(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(_Min(pA.x, pB.x, pC.x), _Min(pA.y, pB.y, pC.y));
        }

        private static Vector2 _Max(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(_Max(pA.x, pB.x, pC.x), _Max(pA.y, pB.y, pC.y));
        }

        protected override void OnDestroy()
        {
            base.graphic.material = null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartInit();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            base.graphic.material = null;
        }
    }
}