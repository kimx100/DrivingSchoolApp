using DrivingSchoolApp.DTOs.Common;
using DrivingSchoolApp.DTOs.CompletedCourse;
using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.TheoryLesson;
using DrivingSchoolApp.DTOs.ValueObject;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class StudentService : ApiService, IStudentService
{
    private readonly IMemoryCache _cache;
    
    public StudentService(IConfiguration config, IMemoryCache cache) : base(config["api_base_url"]!)
    {
        _cache = cache;
    }
    
    public async Task<RestResponse<List<StudentDto>>> GetAllStudentsAsync(bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue("/student", out RestResponse<List<StudentDto>>? students)
            && students is not null) return students;
        
        var request = new RestRequest("/student");
        var response = await ExecuteRequestAsync<List<StudentDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set("/student", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<List<TheoryLessonDto>>> GetAllTheoryLessonsAsync(Guid studentId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/student/{studentId}/theoryLesson", out RestResponse<List<TheoryLessonDto>>? theoryLessons)
            && theoryLessons is not null) return theoryLessons;

        var request = new RestRequest($"/student/{studentId}/theoryLesson");
        var response = await ExecuteRequestAsync<List<TheoryLessonDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/student/{studentId}/theoryLesson", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<List<DrivingLessonDto>>> GetAllDrivingLessonsAsync(Guid studentId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/student/{studentId}/drivingLesson", out RestResponse<List<DrivingLessonDto>>? drivingLessons)
            && drivingLessons is not null) return drivingLessons;

        var request = new RestRequest($"/student/{studentId}/drivingLesson");
        var response = await ExecuteRequestAsync<List<DrivingLessonDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/student/{studentId}/drivingLesson", response, cacheEntryOptions);
        
        return response;
    }

    public async Task<RestResponse<List<StudentDto>>> CreateStudentAsync(StudentRegistryDto registryDto)
    {
        var request = new RestRequest("/student", Method.Post);
        request.AddJsonBody(registryDto);

        return await ExecuteRequestAsync<List<StudentDto>>(request);
    }

    public async Task<RestResponse> DeleteStudentAsync(Guid studentId)
    {
        var request = new RestRequest($"/student/{studentId}", Method.Delete);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse<StudentDto>> GetStudentByIdAsync(Guid studentId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/student/{studentId}", out RestResponse<StudentDto>? student)
            && student is not null) return student;

        var request = new RestRequest($"/student/{studentId}");
        var response = await ExecuteRequestAsync<StudentDto>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/student/{studentId}", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<StudentDto>> UpdateStudentAsync(Guid studentId, StudentUpdateDto updateDto)
    {
        var request = new RestRequest($"/student/{studentId}", Method.Put);
        request.AddJsonBody(updateDto);

        return await ExecuteRequestAsync<StudentDto>(request);
    }

    public async Task<RestResponse> UpdateStudentPasswordAsync(Guid studentId, UpdatePasswordDto updateDto)
    {
        var request = new RestRequest($"/student/{studentId}/password", Method.Put);
        request.AddJsonBody(updateDto);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse<CompletedCourseDto>> GetCompletedCourseByIdAsync(Guid studentId, Guid courseId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/student/{studentId}/course/{courseId}", out RestResponse<CompletedCourseDto>? course)
            && course is not null) return course;

        var request = new RestRequest($"/student/{studentId}/course/{courseId}");
        var response = await ExecuteRequestAsync<CompletedCourseDto>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/student/{studentId}/course/{courseId}", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<List<CompletedCourseDto>>> GetAllCompletedCoursesAsync(Guid studentId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/student/{studentId}/course", out RestResponse<List<CompletedCourseDto>>? courses)
            && courses is not null) return courses;

        var request = new RestRequest($"/student/{studentId}/course");
        var response = await ExecuteRequestAsync<List<CompletedCourseDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/student/{studentId}/course", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<CompletedCourseDto>> CreateCompletedCourseAsync(Guid studentId, CompletedCourseRegistryDto registryDto)
    {
        var request = new RestRequest($"/student/{studentId}/course", Method.Post);
        request.AddJsonBody(registryDto);

        return await ExecuteRequestAsync<CompletedCourseDto>(request);
    }

    public async Task<RestResponse<TimeSlotDto>> AddCalenderTimeSlotAsync(Guid studentId, TimeSlotDto timeSlotDto)
    {
        var request = new RestRequest($"/student/{studentId}/calender", Method.Post);
        request.AddJsonBody(timeSlotDto);

        return await ExecuteRequestAsync<TimeSlotDto>(request);
    }

    public async Task<RestResponse<StudentCalenderDto>> GetCalenderAsync(Guid studentId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/student/{studentId}/calender", out RestResponse<StudentCalenderDto>? courses)
            && courses is not null) return courses;

        var request = new RestRequest($"/student/{studentId}/calender");
        var response = await ExecuteRequestAsync<StudentCalenderDto>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/student/{studentId}/calender", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse> RemoveCalenderTimeSlotAsync(Guid studentId, TimeSlotDto timeSlotDto)
    {
        var request = new RestRequest($"/student/{studentId}/calender", Method.Delete);
        request.AddJsonBody(timeSlotDto);

        return await ExecuteRequestAsync(request);
    }
}