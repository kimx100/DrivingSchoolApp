using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.Mapper.ValueObject;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Mapper;

public static class StudentMapper
{
    extension(StudentDto dto)
    {
        public Student ToModel()
        {
            return new Student(
            dto.Id,
            dto.SchoolId,
            dto.StudentName.ToModel(),
            dto.EmailAddress,
            dto.PhoneNumber);
        }
    }

    extension(Student model)
    {
        public StudentDto ToDto()
        {
            return new StudentDto(
                model.Id,
                model.SchoolId,
                model.StudentName.ToDto(),
                model.EmailAddress,
                model.PhoneNumber
            );
        }
    }
}
