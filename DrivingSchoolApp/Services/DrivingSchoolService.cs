using DrivingSchoolApp.DTOs.DrivingSchool;
using RestSharp;

namespace DrivingSchoolApp.Services;

public class DrivingSchoolService(string baseUrl)
{
    private readonly RestClient _client = new RestClient(baseUrl);
    public async Task<List<DrivingSchoolDto>> GetDrivingSchools()
    {
        var request = new RestRequest($"/drivingschool", Method.Get);

        var response = await _client.ExecuteAsync<List<DrivingSchoolDto>>(request);
        return response.Data ?? [];
    }
    
    
}
