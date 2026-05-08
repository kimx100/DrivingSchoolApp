using DrivingSchoolApp.DTOs.DrivingLesson;
using DrivingSchoolApp.DTOs.DrivingSchool;
using DrivingSchoolApp.DTOs.Instructor;
using DrivingSchoolApp.DTOs.Student;
using DrivingSchoolApp.DTOs.TheoryLesson;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace DrivingSchoolApp.Services.API.Implementation;

public class DrivingSchoolService : ApiService, IDrivingSchoolService
{
    private readonly IMemoryCache _cache;
    
    public DrivingSchoolService(IConfiguration config, IMemoryCache cache) : base(config["api_base_url"]!)
    {
        _cache = cache;
    }
    
    public async Task<RestResponse<DrivingSchoolDto>> GetDrivingSchoolByIdAsync(Guid schoolId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/drivingSchool/{schoolId}", out RestResponse<DrivingSchoolDto>? drivingSchool)
            && drivingSchool is not null) return drivingSchool;
        
        var request = new RestRequest($"/drivingSchool/{schoolId}");
        var response = await ExecuteRequestAsync<DrivingSchoolDto>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/drivingSchool/{schoolId}", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<DrivingSchoolRatingDto>> GetDrivingSchoolRatingAsync(Guid schoolId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/drivingSchool/{schoolId}", out RestResponse<DrivingSchoolRatingDto>? rating)
            && rating is not null) return rating;

        var request = new RestRequest($"/drivingSchool/{schoolId}/rating");
        var response =  await ExecuteRequestAsync<DrivingSchoolRatingDto>(request);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/drivingSchool/{schoolId}/rating", response, cacheEntryOptions);
        
        return response;
    }

    public async Task<RestResponse<DrivingSchoolDto>> CreateDrivingSchoolAsync(DrivingSchoolRegistryDto registryDto)
    {
        var request = new RestRequest("/drivingSchool", Method.Post);
        request.AddJsonBody(registryDto);

        return await ExecuteRequestAsync<DrivingSchoolDto>(request);
    }

    public async Task<RestResponse<List<DrivingSchoolDto>>> GetAllDrivingSchoolsAsync(bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue("/drivingSchool", out RestResponse<List<DrivingSchoolDto>>? schools)
            && schools is not null) return schools;

        var request = new RestRequest("/drivingSchool");
        var response = await ExecuteRequestAsync<List<DrivingSchoolDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set("/drivingSchool", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<List<StudentDto>>> GetAllStudentsFromSchoolAsync(Guid schoolId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/drivingSchool/{schoolId}/student", out RestResponse<List<StudentDto>>? students)
            && students is not null) return students;

        var request = new RestRequest($"/drivingSchool/{schoolId}/student");
        var response = await ExecuteRequestAsync<List<StudentDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/drivingSchool/{schoolId}/student", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<StudentInviteDto>> CreateInviteAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}/student/invite", Method.Post);

        return await ExecuteRequestAsync<StudentInviteDto>(request);
    }

    public async Task<RestResponse> DeleteDrivingSchoolAsync(Guid schoolId)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}", Method.Delete);

        return await ExecuteRequestAsync(request);
    }

    public async Task<RestResponse<List<TheoryLessonDto>>> GetAllTheoryLessonsFromSchoolAsync(Guid schoolId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/drivingSchool/{schoolId}/theoryLesson", out RestResponse<List<TheoryLessonDto>>? theoryLessons)
            && theoryLessons is not null) return theoryLessons;

        var request = new RestRequest($"/drivingSchool/{schoolId}/theoryLesson");
        var response = await ExecuteRequestAsync<List<TheoryLessonDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/drivingSchool/{schoolId}/theoryLesson", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<List<DrivingLessonDto>>> GetAllDrivingLessonsFromSchoolAsync(Guid schoolId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/drivingSchool/{schoolId}/drivingLesson", out RestResponse<List<DrivingLessonDto>>? drivingLessons)
            && drivingLessons is not null) return drivingLessons;

        var request = new RestRequest($"/drivingSchool/{schoolId}/drivingLesson");
        var response = await ExecuteRequestAsync<List<DrivingLessonDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/drivingSchool/{schoolId}/drivingLesson", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<List<InstructorDto>>> GetAllInstructorsFromSchoolAsync(Guid schoolId, bool checkCache = true)
    {
        if (checkCache 
            && _cache.TryGetValue($"/drivingSchool/{schoolId}/instructor", out RestResponse<List<InstructorDto>>? instructors)
            && instructors is not null) return instructors;

        var request = new RestRequest($"/drivingSchool/{schoolId}/instructor");
        var response = await ExecuteRequestAsync<List<InstructorDto>>(request);
        
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DateTime.Now.AddMinutes(30));
        _cache.Set($"/drivingSchool/{schoolId}/instructor", response, cacheEntryOptions);

        return response;
    }

    public async Task<RestResponse<DrivingSchoolDto>> UpdateDrivingSchoolAsync(Guid schoolId, DrivingSchoolUpdateDto updateDto)
    {
        var request = new RestRequest($"/drivingSchool/{schoolId}", Method.Put);
        request.AddJsonBody(updateDto);
        
        return await ExecuteRequestAsync<DrivingSchoolDto>(request);
    }
}