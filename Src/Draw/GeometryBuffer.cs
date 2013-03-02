﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gondola.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gondola.Draw {
    internal class GeometryBuffer<T> : BaseGeometryBuffer<T> where T : struct {
        public Vector3 Rotation{get; private set;}
        public Vector3 Translation { get; private set; }

        public GeometryBuffer(
            int numIndicies,
            int numVerticies,
            int numPrimitives,
            string settingsFileName,
            PrimitiveType primitiveType = PrimitiveType.TriangleList,
            CullMode cullMode = CullMode.None
            )
            : base(numIndicies, numVerticies, numPrimitives, settingsFileName, primitiveType, cullMode) {
                Rotation = new Vector3();
                Translation = new Vector3();

        }

        public IndexBuffer IndexBuffer {
            get { return base.BaseIndexBuffer; }
        }

        public VertexBuffer VertexBuffer {
            get { return base.BaseVertexBuffer; }
        }

        public CullMode CullMode {
            set { Rasterizer = new RasterizerState { CullMode = value }; }
        }

        public T[] DumpVerticies() {
            T[] data = new T[BaseVertexBuffer.VertexCount];
            base.BaseVertexBuffer.GetData(data);
            return data;
        }

        public int[] DumpIndicies() {
            int[] data = new int[BaseIndexBuffer.IndexCount];
            base.BaseIndexBuffer.GetData(data);
            return data;
        }

        public void Translate(Vector3 diff){
            Translation += diff;
            UpdateWorldMatrix();
        }

        public void Rotate(Angle3 diff) {
            Rotation += diff.ToVec();
            UpdateWorldMatrix();
        }

        void UpdateWorldMatrix(){
            BaseWorldMatrix = Matrix.Identity;
            BaseWorldMatrix *= Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y) * Matrix.CreateRotationZ(Rotation.Z);
            BaseWorldMatrix *= Matrix.CreateTranslation(Translation.X, Translation.Y, Translation.Z);
        }
    }
}
