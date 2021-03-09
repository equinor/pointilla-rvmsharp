﻿using Equinor.MeshOptimizationPipeline;
using rvmsharp.Rvm;
using rvmsharp.Rvm.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace rvmsharp.Tessellator
{
    public class TessellatorBridge
    {
        public static Mesh Tessellate(RvmPrimitive geometry, float scale)
        {
            return geometry switch
            {
                RvmBox box => Tessellate(box, scale),
                RvmFacetGroup facetGroup => Tessellate(facetGroup, scale),
                _ => throw new NotImplementedException($"Unsupported type for tesselation: {geometry.Kind}"),
            };
        }

        private static int triIndices(int[] indices, int l, int o, int v0, int v1, int v2)
        {
            indices[l++] = o + v0;
            indices[l++] = o + v1;
            indices[l++] = o + v2;
            return l;
        }


        private static int quadIndices(int[] indices, int l, int o, int v0, int v1, int v2, int v3)
        {
            indices[l++] = o + v0;
            indices[l++] = o + v1;
            indices[l++] = o + v2;

            indices[l++] = o + v2;
            indices[l++] = o + v3;
            indices[l++] = o + v0;
            return l;
        }

        private static int vertex(float[] normals, float[] vertices, int l, Vector3 n, Vector3 p)
        {
            normals[l] = n.X;
            vertices[l++] = p.X;
            normals[l] = n.Y;
            vertices[l++] = p.Y;
            normals[l] = n.Z;
            vertices[l++] = p.Z;
            return l;
        }

        private static int vertex(float[] normals, float[] vertices, int l, float nx, float ny, float nz, float px,
            float py, float pz)
        {
            normals[l] = nx;
            vertices[l++] = px;
            normals[l] = ny;
            vertices[l++] = py;
            normals[l] = nz;
            vertices[l++] = pz;
            return l;
        }

        private class Interface
        {
            public enum Kind
            {
                Undefined,
                Square,
                Circular
            }

            public Kind kind = Kind.Undefined;
            public Vector3[] p = new Vector3[4];
            public float radius;
        }

        private static Interface GetInterface(RvmPrimitive geo, int o)
        {
            var iface = new Interface();
            var connection = geo.Connections[o];
            var ix = connection.p1 == geo ? 1 : 0;
            if (!Matrix4x4.Decompose(geo.Matrix, out var vscale, out var _, out var _))
                throw new Exception();
            var scale = Math.Max(vscale.X, Math.Max(vscale.Y, vscale.Z));
            switch (geo)
            {
                case RvmPyramid pyramid:
                {
                    var bx = 0.5f * pyramid.BottomX;
                    var by = 0.5f * pyramid.BottomY;
                    var tx = 0.5f * pyramid.TopX;
                    var ty = 0.5f * pyramid.TopY;
                    var ox = 0.5f * pyramid.OffsetX;
                    var oy = 0.5f * pyramid.OffsetY;
                    var h2 = 0.5f * pyramid.Height;
                    Vector3[,] quad = new Vector3[,]
                    {
                        {
                            new Vector3(-bx - ox, -by - oy, -h2), new Vector3(bx - ox, -by - oy, -h2),
                            new Vector3(bx - ox, by - oy, -h2), new Vector3(-bx - ox, by - oy, -h2)
                        },
                        {
                            new Vector3(-tx + ox, -ty + oy, h2), new Vector3(tx + ox, -ty + oy, h2),
                            new Vector3(tx + ox, ty + oy, h2), new Vector3(-tx + ox, ty + oy, h2)
                        },
                    };

                    iface.kind = Interface.Kind.Square;
                    if (o < 4)
                    {
                        var oo = (o + 1) & 3;
                        iface.p[0] = Vector3.Transform(quad[0, o], geo.Matrix);
                        iface.p[1] = Vector3.Transform(quad[0, oo], geo.Matrix);
                        iface.p[2] = Vector3.Transform(quad[1, oo], geo.Matrix);
                        iface.p[3] = Vector3.Transform(quad[1, o], geo.Matrix);
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++) iface.p[k] = Vector3.Transform(quad[o - 4, k], geo.Matrix);
                    }

                    break;
                }
                case RvmBox box:
                {
                    var xp = 0.5f * box.LengthX;
                    var xm = -xp;
                    var yp = 0.5f * box.LengthY;
                    var ym = -yp;
                    var zp = 0.5f * box.LengthZ;
                    var zm = -zp;
                    Vector3[,] V =
                    {
                        {
                            new Vector3(xm, ym, zp), new Vector3(xm, yp, zp), new Vector3(xm, yp, zm),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xp, ym, zm), new Vector3(xp, yp, zm), new Vector3(xp, yp, zp),
                            new Vector3(xp, ym, zp)
                        },
                        {
                            new Vector3(xp, ym, zm), new Vector3(xp, ym, zp), new Vector3(xm, ym, zp),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xm, yp, zm), new Vector3(xm, yp, zp), new Vector3(xp, yp, zp),
                            new Vector3(xp, yp, zm)
                        },
                        {
                            new Vector3(xm, yp, zm), new Vector3(xp, yp, zm), new Vector3(xp, ym, zm),
                            new Vector3(xm, ym, zm)
                        },
                        {
                            new Vector3(xm, ym, zp), new Vector3(xp, ym, zp), new Vector3(xp, yp, zp),
                            new Vector3(xm, yp, zp)
                        }
                    };
                    for (var k = 0; k < 4; k++) iface.p[k] = Vector3.Transform(V[o, k], geo.Matrix);
                    break;
                }
                case RvmRectangularTorus tor:
                {
                    var h2 = 0.5f * tor.Height;
                    float[,] square =
                    {
                        {tor.RadiusOuter, -h2}, {tor.RadiusInner, -h2}, {tor.RadiusInner, h2},
                        {tor.RadiusOuter, h2},
                    };
                    if (o == 0)
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            iface.p[k] = Vector3.Transform(new Vector3(square[k, 0], 0.0f, square[k, 1]), geo.Matrix);
                        }
                    }
                    else
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            iface.p[k] = Vector3.Transform(new Vector3((float)(square[k, 0] * Math.Cos(tor.Angle)),
                                (float)(square[k, 0] * Math.Sin(tor.Angle)),
                                square[k, 1]), geo.Matrix);
                        }
                    }

                    break;
                }
                case RvmCircularTorus circularTorus:
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * circularTorus.Radius;
                    break;

                case RvmEllipticalDish ellipticalDish:
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * ellipticalDish.BaseRadius;
                    break;

                case RvmSphericalDish sphericalDish:
                {
                    float r_circ = sphericalDish.BaseRadius;
                    var h = sphericalDish.Height;
                    float r_sphere = (r_circ * r_circ + h * h) / (2.0f * h);
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * r_sphere;
                    break;
                }
                case RvmSnout snout:
                    iface.kind = Interface.Kind.Circular;
                    var offset = ix == 0 ? connection.OffsetX : connection.OffsetY;
                    iface.radius = scale * (offset == 0 ? snout.RadiusBottom : snout.RadiusTop);
                    break;
                case RvmCylinder cylinder:
                    iface.kind = Interface.Kind.Circular;
                    iface.radius = scale * cylinder.Radius;
                    break;
                case RvmSphere:
                case RvmLine:
                case RvmFacetGroup:
                    iface.kind = Interface.Kind.Undefined;
                    break;
                default:
                    throw new NotSupportedException("Unhandled primitive type");
            }

            return iface;
        }

        private static bool DoInterfacesMatch(RvmPrimitive geo, RvmConnection con)
        {
            bool isFirst = geo == con.p1;

            var thisGeo = isFirst ? con.p1 : con.p2;
            var thisOffset = isFirst ? con.OffsetX : con.OffsetY;
            var thisIFace = GetInterface(thisGeo, (int)thisOffset);

            var thatGeo = isFirst ? con.p2 : con.p1;
            var thatOffset = isFirst ? con.OffsetY : con.OffsetX;
            var thatIFace = GetInterface(thatGeo, (int)thatOffset);


            if (thisIFace.kind != thatIFace.kind) return false;

            if (thisIFace.kind == Interface.Kind.Circular)
            {
                return thisIFace.radius <= 1.05f * thatIFace.radius;
            }
            else
            {
                for (var j = 0; j < 4; j++)
                {
                    bool found = false;
                    for (var i = 0; i < 4; i++)
                    {
                        if (Vector3.DistanceSquared(thisIFace.p[j], thatIFace.p[i]) < 0.001f * 0.001f)
                        {
                            found = true;
                        }
                    }

                    if (!found) return false;
                }

                return true;
            }
        }

        private static Mesh Tessellate(RvmBox box, float scale)
        {
            var xp = 0.5f * box.LengthX;
            var xm = -xp;
            var yp = 0.5f * box.LengthY;
            var ym = -yp;
            var zp = 0.5f * box.LengthZ;
            var zm = -zp;

            Vector3[,] V = new Vector3[,]
            {
                {
                    new Vector3(xm, ym, zp), new Vector3(xm, yp, zp), new Vector3(xm, yp, zm), new Vector3(xm, ym, zm)
                },
                {new Vector3(xp, ym, zm), new Vector3(xp, yp, zm), new Vector3(xp, yp, zp), new Vector3(xp, ym, zp)},
                {new Vector3(xp, ym, zm), new Vector3(xp, ym, zp), new Vector3(xm, ym, zp), new Vector3(xm, ym, zm)},
                {new Vector3(xm, yp, zm), new Vector3(xm, yp, zp), new Vector3(xp, yp, zp), new Vector3(xp, yp, zm)},
                {new Vector3(xm, yp, zm), new Vector3(xp, yp, zm), new Vector3(xp, ym, zm), new Vector3(xm, ym, zm)},
                {new Vector3(xm, ym, zp), new Vector3(xp, ym, zp), new Vector3(xp, yp, zp), new Vector3(xm, yp, zp)}
            };

            Vector3[] N =
            {
                new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 1, 0),
                new Vector3(0, 0, -1), new Vector3(0, 0, 1)
            };

            bool[] faces =
            {
                1e-5 <= box.LengthX, 1e-5 <= box.LengthX, 1e-5 <= box.LengthY, 1e-5 <= box.LengthY,
                1e-5 <= box.LengthZ, 1e-5 <= box.LengthZ,
            };

            for (var i = 0; i < 6; i++)
            {
                var con = box.Connections[i];
                if (faces[i] == false || con == null || con.flags != RvmConnection.Flags.HasRectangularSide) continue;

                if (DoInterfacesMatch(box, con))
                {
                    faces[i] = false;
                    //store.addDebugLine(con.p.data, (con.p.data + 0.05f*con.d).data, 0xff0000);
                }
            }

            var faces_n = 0;
            for (var i = 0; i < 6; i++)
            {
                if (faces[i]) faces_n++;
            }

            

            if (faces_n > 0)
            {
                var vertices_n = 4 * faces_n;
                var vertices = new float[3 * vertices_n];
                var normals = new float[3 * vertices_n];

                var triangles_n = 2 * faces_n;
                var indices = new int[3 * triangles_n];

                var o = 0;
                var i_v = 0;
                var i_p = 0;
                for (var f = 0; f < 6; f++)
                {
                    if (!faces[f]) continue;

                    for (var i = 0; i < 4; i++)
                    {
                        i_v = vertex(normals, vertices, i_v, N[f], V[f, i]);
                    }

                    i_p = quadIndices(indices, i_p, o, 0, 1, 2, 3);

                    o += 4;
                }

                var tri = new Mesh(vertices, normals, indices, 0.0f);

                if (!(i_v == 3 * vertices_n) ||
                    !(i_p == 3 * triangles_n) ||
                    !(o == vertices_n))
                {
                    throw new Exception();
                }
                return tri;
            }

            return new Mesh(new float[0], new float[0], new int[0], 0);
        }

        [DllImport("tessbridge", CallingConvention = CallingConvention.Cdecl, EntryPoint = "tessellate")]
        private static extern int tessellate(float[] vertex_data, float[] normal_data, int[] vertex_counts, int counter_count, out int out_vertex_count, out int out_normal_count, out int out_index_count);

        [DllImport("tessbridge", CallingConvention = CallingConvention.Cdecl, EntryPoint = "collect_result")]
        private static extern void collect_result(int job_id, float[] vertex_buffer, float[] normal_buffer, int[] triangle_buffer);

        private static Mesh Tessellate(RvmFacetGroup facetGroup, float scale)
        {
            var vertices = new List<float>();
            var normals = new List<float>();
            var indices = new List<int>();

            for (var p = 0; p < facetGroup._polygons.Length; p++)
            {
                var poly = facetGroup._polygons[p];
                if (poly._contours.Length == 1 && poly._contours[0]._vertices.Length == 3)
                {
                    var cont = poly._contours[0];
                    var vo = vertices.Count / 3;

                    for (var v = 0; v < cont._vertices.Length;v++)
                    {
                        var vv = cont._vertices[v];
                        vertices.Add(vv.v.X);
                        vertices.Add(vv.v.Y);
                        vertices.Add(vv.v.Z);
                        normals.Add(vv.n.X);
                        normals.Add(vv.n.Y);
                        normals.Add(vv.n.Z);
                    }

                    indices.Add(vo + 0);
                    indices.Add(vo + 1);
                    indices.Add(vo + 2);
                } else if (poly._contours.Length == 1 && poly._contours[0]._vertices.Length == 4)
                {
                    var cont = poly._contours[0];
                    var V = cont._vertices;
                    var vo = vertices.Count / 3;

                    for (var v = 0; v < 4; v++)
                    {
                        var vv = cont._vertices[v];
                        vertices.Add(vv.v.X);
                        vertices.Add(vv.v.Y);
                        vertices.Add(vv.v.Z);
                        normals.Add(vv.n.X);
                        normals.Add(vv.n.Y);
                        normals.Add(vv.n.Z);
                    }

                    // find least folding diagonal
                    var v01 = V[1].v - V[0].v;
                    var v12 = V[2].v - V[1].v;
                    var v23 = V[3].v - V[2].v;
                    var v30 = V[0].v - V[3].v;
                    var n0 = Vector3.Cross(v01, v30);
                    var n1 = Vector3.Cross(v12, v01);
                    var n2 = Vector3.Cross(v23, v12);
                    var n3 = Vector3.Cross(v30, v23);

                    if (Vector3.Dot(n0, n2) < Vector3.Dot(n1, n3))
                    {
                        indices.Add(vo + 0);
                        indices.Add(vo + 1);
                        indices.Add(vo + 2);

                        indices.Add(vo + 2);
                        indices.Add(vo + 3);
                        indices.Add(vo + 0);
                    }
                    else
                    {
                        indices.Add(vo + 3);
                        indices.Add(vo + 0);
                        indices.Add(vo + 1);

                        indices.Add(vo + 1);
                        indices.Add(vo + 2);
                        indices.Add(vo + 3);
                    }
                } else {
                    var (bMin, bMax) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
                    foreach (var cont in poly._contours)
                    {
                        foreach (var vn in cont._vertices)
                        {
                            (bMin.X, bMin.Y, bMin.Z) = (Math.Min(bMin.X, vn.v.X), Math.Min(bMin.Y, vn.v.Y), Math.Min(bMin.Z, vn.v.Z));
                            (bMax.X, bMax.Y, bMax.Z) = (Math.Max(bMax.X, vn.v.X), Math.Max(bMax.Y, vn.v.Y), Math.Max(bMax.Z, vn.v.Z));
                        }
                    }
                    var m = 0.5f * (bMin + bMax);

                    var vo = vertices.Count / 3;
                    float[] vertex_data = poly._contours.SelectMany(c => c._vertices).Select(vn => vn.v - m).SelectMany(v => new[] { v.X, v.Y, v.Z }).ToArray();
                    float[] normal_data = poly._contours.SelectMany(c => c._vertices).Select(vn => vn.n).SelectMany(v => new[] { v.X, v.Y, v.Z }).ToArray();
                    int[] vertex_counts = poly._contours.Select(c => c._vertices.Length).ToArray();
                    int counter_count = poly._contours.Length;

                    var jobId = tessellate(vertex_data, normal_data, vertex_counts, counter_count, out var out_vertex_count, out var out_normal_count, out var out_index_count);
                    if (jobId < 0) throw new Exception();
                    var out_vertex_data = new float[out_vertex_count];
                    var out_normal_data = new float[out_normal_count];
                    var out_index_data = new int[out_index_count];
                    collect_result(jobId, out_vertex_data, out_normal_data, out_index_data);

                    for (var i = 0; i < out_vertex_data.Length / 3; i++)
                    {
                        out_vertex_data[i*3 + 0] += m.X;
                        out_vertex_data[i*3 + 1] += m.Y;
                        out_vertex_data[i*3 + 2] += m.Z;
                    }

                    vertices.AddRange(out_vertex_data);
                    normals.AddRange(out_normal_data);

                    foreach (var index in out_index_data)
                    {
                        indices.Add(index + vo);
                    }

                    if (vertices.Count != normals.Count)
                        throw new Exception();

                }
            }

            return new Mesh(vertices.ToArray(), normals.ToArray(), indices.ToArray(), 0);
        }
    }
}