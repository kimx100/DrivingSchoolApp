using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.Mapper.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper;

public static class InstructorMapper
{
    extension(InstructorDto dto)
    {
        public Instructor ToModel()
        {
            return new Instructor(
                dto.Id,
                dto.SchoolId,
                dto.Name.ToModel(),
                dto.EmailAddress,
                dto.PhoneNumber,
                dto.TheoryLessonIDs,
                dto.DrivingLessonIds
            );
        }
    }
    
    extension(Instructor model)
    {
        public InstructorDto ToDto()
        {
            return new InstructorDto(
                model.Id,
                model.SchoolId,
                model.Name.ToDto(),
                model.EmailAddress,
                model.PhoneNumber,
                model.TheoryLessonIDs,
                model.DrivingLessonIds);
        }
    }
}
