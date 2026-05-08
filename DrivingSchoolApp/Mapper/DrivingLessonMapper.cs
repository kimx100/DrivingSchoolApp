using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.Mapper.ValueObject;
using DrivingSchoolApp.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace DrivingSchoolApp.Mapper;

public static class DrivingLessonMapper
{

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
            using var instructorSignature = new Image<A8>(signatureWidth, signatureHeight);

            var instructorPoints = model.InstructorSignature?.Strokes.SelectMany(x => x.Points)
                .Select(p => new PointF(p.X, p.Y)).ToArray() ?? [];
            instructorSignature.Mutate(o =>
                o.DrawLine(Color.Black, model.InstructorSignature!.Strokes[0].LineWidth, instructorPoints)
            );
            
            using var studentSignature = new Image<A8>(signatureWidth, signatureHeight);
            var studentPoints = model.StudentSignature?.Strokes.SelectMany(x => x.Points)
                .Select(p => new PointF(p.X, p.Y)).ToArray() ?? [];
            studentSignature.Mutate(o =>
                o.DrawLine(Color.Black, model.StudentSignature!.Strokes[0].LineWidth, studentPoints)
            );

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
}