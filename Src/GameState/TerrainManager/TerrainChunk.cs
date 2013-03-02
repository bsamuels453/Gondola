﻿using System;
using System.Diagnostics;
using System.Linq;
using Gondola.Draw;
using Gondola.Util;
using Microsoft.Xna.Framework.Graphics;

namespace Gondola.GameState.TerrainManager {
    class TerrainChunk : IDisposable{
        public XZPair Identifier;

        readonly GeometryBuffer<VertexPositionTexture> _buffer;
        readonly GeometryBuffer<VertexPositionTexture> _wbuff;
        readonly VertexPositionTexture[] _verticies;
        readonly int[] _indicies;
        readonly Texture2D _normals;
        readonly Texture2D _binormals;
        readonly Texture2D _tangents;

        bool _bufferDataSet;

        public TerrainChunk(XZPair identifier, VertexPositionTexture[] verticies, int[] indicies, Texture2D normals, Texture2D binormals, Texture2D tangents){
            Identifier = identifier;
            _verticies = verticies;
            _indicies = indicies;
            _normals = normals;
            _binormals = binormals;
            _tangents = tangents;
            _buffer = new GeometryBuffer<VertexPositionTexture>(indicies.Length, verticies.Count(), indicies.Count()/3, "Shader_Terrain");
            _wbuff = new GeometryBuffer<VertexPositionTexture>(indicies.Count()*2, verticies.Count(), indicies.Count(), "Shader_Wireframe", PrimitiveType.LineList);
            _wbuff.ShaderParams["Alpha"].SetValue(0.25f);

        Debug.Assert(_bufferDataSet == false);
            _buffer.IndexBuffer.SetData((int[])_indicies.Clone());
            _buffer.VertexBuffer.SetData(_verticies);
            _buffer.ShaderParams["NormalMapTexture"].SetValue(_normals);
            _buffer.ShaderParams["BinormalMapTexture"].SetValue(_binormals);
            _buffer.ShaderParams["TangentMapTexture"].SetValue(_tangents);

            //we need to explode the indice list from triangle list to line list
            var wireframeInds = new int[_indicies.Length * 2];
            int srcIdx = 0;
            for (int i = 0; i < _indicies.Length * 2; i += 6) {
                wireframeInds[i] = _indicies[srcIdx];
                wireframeInds[i + 1] = _indicies[srcIdx + 1];
                wireframeInds[i + 2] = _indicies[srcIdx + 1];
                wireframeInds[i + 3] = _indicies[srcIdx + 2];
                wireframeInds[i + 4] = _indicies[srcIdx + 2];
                wireframeInds[i + 5] = _indicies[srcIdx];
                srcIdx += 3;
            }

            _wbuff.IndexBuffer.SetData(wireframeInds);
            _wbuff.VertexBuffer.SetData(_verticies);

            _normals.Dispose();
            _binormals.Dispose();
            _tangents.Dispose();
        }

        public void SetBufferData(){

            _bufferDataSet = true;
        }

        public void Dispose(){
            //_normals.Dispose();
            //_binormals.Dispose();
            //_tangents.Dispose();
            _buffer.Dispose();
        }
    }
}
