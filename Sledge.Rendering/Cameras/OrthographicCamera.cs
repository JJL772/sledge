﻿using System;
using System.Globalization;
using System.Numerics;

namespace Sledge.Rendering.Cameras
{
    public class OrthographicCamera : ICamera
    {
        public enum OrthographicType
        {
            Top,
            Front,
            Side
        }

        private static readonly Matrix4x4 TopMatrix = Matrix4x4.Identity;
        //private static readonly Matrix4x4 FrontMatrix = new Matrix4x4(Vector4.UnitZ, Vector4.UnitX, Vector4.UnitY, Vector4.UnitW);
        //private static readonly Matrix4x4 SideMatrix = new Matrix4x4(Vector4.UnitX, Vector4.UnitZ, Vector4.UnitY, Vector4.UnitW);
        private static readonly Matrix4x4 FrontMatrix = new Matrix4x4(
            0, 0, 1, 0,
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 0, 1
        );
        private static readonly Matrix4x4 SideMatrix = new Matrix4x4(
            1, 0, 0, 0,
            0, 0, 1, 0,
            0, 1, 0, 0,
            0, 0, 0, 1
        );
        private Vector3 _position;
        private float _zoom;

        public CameraType Type => CameraType.Orthographic;
        public int Width { get; set; }
        public int Height { get; set; }
        public Vector3 Location => EyeLocation;
        public Matrix4x4 View => GetCameraMatrix();
        public Matrix4x4 Projection => GetViewportMatrix(Width, Height);

        private static Matrix4x4 GetMatrixFor(OrthographicType dir)
        {
            switch (dir)
            {
                case OrthographicType.Top:
                    return TopMatrix;
                case OrthographicType.Front:
                    return FrontMatrix;
                case OrthographicType.Side:
                    return SideMatrix;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir));
            }
        }

        public Vector3 Position
        {
            get => _position;
            set
            {
                const float u = 131072;
                const float l = -131072;
                _position = new Vector3(Math.Min(u, Math.Max(l, value.X)), Math.Min(u, Math.Max(l, value.Y)), Math.Min(u, Math.Max(l, value.Z)));
            }
        }

        public float Zoom
        {
            get => _zoom;
            set => _zoom = Math.Min(256, Math.Max(0.001f, value));
        }

        public OrthographicType ViewType { get; set; }

        public OrthographicCamera(OrthographicType type)
        {
            ViewType = type;
            Position = Vector3.Zero;
            Zoom = 1;
        }

        internal OrthographicCamera(string serialised) : this(OrthographicType.Top)
        {
            var tags = (serialised ?? "").Split(',', '/');
            if (tags.Length < 1) return;

            if (Enum.TryParse(tags[0], true, out OrthographicType t)) ViewType = t;

            if (tags.Length < 4) return;

            float p, x = 0, y = 0, z = 0;

            if (float.TryParse(tags[1], NumberStyles.Float, CultureInfo.InvariantCulture, out p)) x = p;
            if (float.TryParse(tags[2], NumberStyles.Float, CultureInfo.InvariantCulture, out p)) y = p;
            if (float.TryParse(tags[3], NumberStyles.Float, CultureInfo.InvariantCulture, out p)) z = p;
            Position = new Vector3(x, y, z);

            if (tags.Length < 5) return;
            if (float.TryParse(tags[4], NumberStyles.Float, CultureInfo.InvariantCulture, out p)) Zoom = p;
        }

        public Vector3 EyeLocation => (Vector3.UnitZ * float.MaxValue) + _position;

        public Matrix4x4 GetCameraMatrix()
        {
            var translate = Matrix4x4.CreateTranslation(-Position.X, -Position.Y, 0);
            var scale = Matrix4x4.CreateScale(new Vector3(Zoom, Zoom, 0));
            return translate * scale;
        }

        public Matrix4x4 GetViewportMatrix(int width, int height)
        {
            const float near = -1000000;
            const float far = 1000000;
            return Matrix4x4.CreateOrthographic(width, height, near, far);
        }

        public Matrix4x4 GetModelMatrix()
        {
            return GetMatrixFor(ViewType);
        }

        public Vector3 ScreenToWorld(Vector3 screen, int width, int height)
        {
            screen = new Vector3(screen.X, height - screen.Y, screen.Z);
            var cs = new Vector3(width / 2f, height / 2f, 0);
            var flat = Position + ((screen - cs) / Zoom);
            return Expand(flat);
        }

        public Vector3 WorldToScreen(Vector3 world, int width, int height)
        {
            var flat = Flatten(world);
            var cs = new Vector3(width / 2f, height / 2f, 0);
            var screen = cs + ((flat - Position) * Zoom);
            return new Vector3(screen.X, height - screen.Y, screen.Z);
        }

        //public override Line CastRayFromScreen(Vector3 screen, int width, int height)
        //{
        //    screen = new Vector3(screen.X, screen.Y, 0);
        //    var world = ScreenToWorld(screen, width, height);
        //    return null; // todo
        //}

        //public override IEnumerable<Plane> GetClippingPlanes(int width, int height)
        //{
        //    var min = ScreenToWorld(Vector3.Zero, width, height);
        //    var max = ScreenToWorld(new Vector3(width, height, 0), width, height);
        //    yield return new Plane(Expand(Vector3.UnitX), min);
        //    yield return new Plane(-Expand(Vector3.UnitY), min);
        //    yield return new Plane(-Expand(Vector3.UnitX), max);
        //    yield return new Plane(Expand(Vector3.UnitY), max);
        //}

        public float UnitsToPixels(float units)
        {
            return units * Zoom;
        }

        public float PixelsToUnits(float pixels)
        {
            return pixels / Zoom;
        }

        public Vector3 Flatten(Vector3 notFlat)
        {
            switch (ViewType)
            {
                case OrthographicType.Top:
                    return new Vector3(notFlat.X, notFlat.Y, 0);
                case OrthographicType.Front:
                    return new Vector3(notFlat.Y, notFlat.Z, 0);
                case OrthographicType.Side:
                    return new Vector3(notFlat.X, notFlat.Z, 0);
                default:
                    throw new ArgumentOutOfRangeException("Type");
            }
        }

        public Vector3 Expand(Vector3 flat)
        {
            switch (ViewType)
            {
                case OrthographicType.Top:
                    return new Vector3(flat.X, flat.Y, 0);
                case OrthographicType.Front:
                    return new Vector3(0, flat.X, flat.Y);
                case OrthographicType.Side:
                    return new Vector3(flat.X, 0, flat.Y);
                default:
                    throw new ArgumentOutOfRangeException("Type");
            }
        }

        internal string Serialise()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}/{1},{2},{3}/{4}",
                ViewType, Position.X, Position.Y, Position.Z, Zoom);
        }
    }
}
