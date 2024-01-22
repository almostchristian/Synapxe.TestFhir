@Environment:Integration
Feature: Education

Background:
    And a new HttpClient as httpClient
        | HeaderName               | Value   |
        | X-Ihis-SourceApplication | testapp |

Scenario: Reading a newly created education resource returns exactly the same education
    Given a Resource is created from Samples/Education.json as createdEdu
    When getting Education/{createdEdu.Id} as readEdu
    Then createdEdu and readEdu are exactly the same
    
Scenario: Reading a nonexistent education returns 404 status code
    When getting Education/nonexistent as readEdu
    Then readEdu is a Fhir OperationOutcome with data
        | Path       | Value |
        | statusCode | 404   |
        
Scenario: Updating a newly created education resource returns an education with an incremented versionId
    Given a Resource is created from Samples/Education.json as createdEdu
    When updating createdEdu with data as updatedEdu
        | Path  | Value   | FhirType |
        | study | Geology | string   |
    And getting Education/{createdEdu.Id}/_history/2 as readUpdatedEdu
    Then updatedEdu and readUpdatedEdu are exactly the same
    And updatedEdu is a Fhir Education with data
        | Path         | Value |
        | statusCode   | 200   |
        | headers.eTag | W/"2" |
    And readUpdatedEdu is a Fhir Education with data
        | Path           | Value   | FhirType |
        | study          | Geology | string   |
        | meta.versionId | 2       | string   |
        
Scenario: Updating an education resource with a mismatch of id in the body and path returns 400 status code
    Given a Resource is created from Samples/Education.json as createdEdu
    When updating createdEdu with data as updatedEdu
        | Path  | Value     | FhirType |
        | study | Geology   | string   |
        | id    | {newguid} | id       |
    Then updatedEdu is a Fhir OperationOutcome with data
        | Path                            | Value                                                            | FhirType |
        | statusCode                      | 400                                                              |          |
        | issue[0].diagnostics            | Resource ID in resource does not match with Resource ID in path. | string   |
