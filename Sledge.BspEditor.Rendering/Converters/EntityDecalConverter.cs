using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Sledge.BspEditor.Documents;
using Sledge.BspEditor.Primitives.MapObjectData;
using Sledge.BspEditor.Primitives.MapObjects;
using Sledge.BspEditor.Rendering.ChangeHandlers;
using Sledge.DataStructures.Geometric;
using Sledge.Rendering.Engine;
using Sledge.Rendering.Resources;

namespace Sledge.BspEditor.Rendering.Converters
{
    [Export(typeof(IMapObjectSceneConverter))]
    public class EntityDecalConverter : IMapObjectSceneConverter
    {
        [Import] private EngineInterface _engine;

        public MapObjectSceneConverterPriority Priority => MapObjectSceneConverterPriority.DefaultLow;

        public bool ShouldStopProcessing(MapDocument document, IMapObject obj)
        {
            return true;
        }

        public bool Supports(IMapObject obj)
        {
            return obj is Entity && obj.Data.OfType<EntityDecal>().Any();
        }

        public async Task Convert(BufferBuilder builder, MapDocument document, IMapObject obj)
        {
            var faces = obj.Data.Get<EntityDecal>().SelectMany(x => x.Geometry).ToList();
            await DefaultSolidConverter.ConvertFaces(builder, document, obj, faces, _engine);

            var origin = obj.Data.GetOne<Origin>()?.Location ?? obj.BoundingBox.Center;
            await DefaultEntityConverter.ConvertBox(builder, document, obj, new Box(origin - Vector3.One * 4, origin + Vector3.One * 4));
        }
    }
}