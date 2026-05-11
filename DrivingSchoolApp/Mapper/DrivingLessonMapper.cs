using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.Mapper.ValueObject;
using DrivingSchoolApp.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace DrivingSchoolApp.Mapper;

public static class DrivingLessonMapper
{
    private const float DefaultSignatureLineWidth = 4f;

    extension(IEnumerable<CompletedLessonItem> model)
    {
        public DrivingLessonObjectiveDto ToDto()
        {
            var lessonTypes = model.Select(x => x.ItemType).ToHashSet();
            return new DrivingLessonObjectiveDto(
                lessonTypes.Contains(LessonItemType.RightTurn), // Should be something else
                lessonTypes.Contains(LessonItemType.HighwayDriving),
                lessonTypes.Contains(LessonItemType.NightDriving),
                lessonTypes.Contains(LessonItemType.LeftTurn), // Should be something else
                lessonTypes.Contains(LessonItemType.CorenerReversing),
                lessonTypes.Contains(LessonItemType.ParallelParking)
                );
        }
    }
    
    extension(RouteSession model)
    {
        public async Task<DrivingLessonRegistryDto> ToRegistryDto(
            Guid schoolId,
            Money price,
            int signatureWidth, 
            int signatureHeight)
        {
            using var instructorSignature = new Image<A8>(
                Math.Max(signatureWidth, 1),
                Math.Max(signatureHeight, 1));

            DrawSignature(instructorSignature, model.InstructorSignature);
            
            using var studentSignature = new Image<A8>(
                Math.Max(signatureWidth, 1),
                Math.Max(signatureHeight, 1));

            DrawSignature(studentSignature, model.StudentSignature);

            using var instructorSignatureMs = new MemoryStream();
            using var studentSignatureMs = new MemoryStream();

            var instructorSignatureSaveTask = instructorSignature.SaveAsPngAsync(instructorSignatureMs);
            var studentSignatureSaveTask = studentSignature.SaveAsPngAsync(studentSignatureMs);

            await instructorSignatureSaveTask;
            await studentSignatureSaveTask;

            return new DrivingLessonRegistryDto(
                instructorSignatureMs.ToArray(),
                studentSignatureMs.ToArray(),
                schoolId,
                Guid.Parse(model.StudentId!),
                model.ToDto(),
                price.ToDto(),
                model.CompletedItems.ToDto()
            );
        }
    }

    private static void DrawSignature(Image<A8> image, LessonSignature? signature)
    {
        foreach (var stroke in signature?.Strokes ?? [])
        {
            var points = stroke.Points
                .Select(p => new PointF(p.X, p.Y))
                .ToArray();

            if (points.Length == 0)
                continue;

            var lineWidth = stroke.LineWidth > 0 ? stroke.LineWidth : DefaultSignatureLineWidth;

            if (points.Length == 1)
            {
                var point = points[0];
                var radius = Math.Max(lineWidth / 2f, 1f);

                image.Mutate(o => o.Fill(Color.Black, new EllipsePolygon(point.X, point.Y, radius)));
                continue;
            }

            image.Mutate(o => o.DrawLine(Color.Black, lineWidth, points));
        }
    }
}
