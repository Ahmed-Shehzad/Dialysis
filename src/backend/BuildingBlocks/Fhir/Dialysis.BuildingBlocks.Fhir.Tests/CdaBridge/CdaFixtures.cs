namespace Dialysis.BuildingBlocks.Fhir.Tests.CdaBridge;

/// <summary>
/// Representative C-CDA R2.1 fixtures used by the bridge tests. The full CCD carries a complete
/// patient header plus one entry in each of the six supported sections; the sparse variants
/// exercise the parser's null-soft behaviour.
/// </summary>
internal static class CdaFixtures
{
    public const string FullCcd = """
        <?xml version="1.0" encoding="UTF-8"?>
        <ClinicalDocument xmlns="urn:hl7-org:v3" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
          <templateId root="2.16.840.1.113883.10.20.22.1.2"/>
          <code code="34133-9" codeSystem="2.16.840.1.113883.6.1"/>
          <title>Continuity of Care Document</title>
          <effectiveTime value="20260601120000+0000"/>
          <recordTarget>
            <patientRole>
              <id root="2.16.840.1.113883.19.5" extension="MRN-7788"/>
              <id root="2.16.840.1.113883.4.1" extension="999-22-1111"/>
              <addr>
                <streetAddressLine>123 Dialysis Way</streetAddressLine>
                <city>Berlin</city>
                <state>BE</state>
                <postalCode>10115</postalCode>
                <country>DE</country>
              </addr>
              <telecom value="tel:+49-30-1234567"/>
              <telecom value="mailto:ada@example.org"/>
              <patient>
                <name>
                  <prefix>Dr</prefix>
                  <given>Ada</given>
                  <given>Marie</given>
                  <family>Lovelace</family>
                </name>
                <administrativeGenderCode code="F" codeSystem="2.16.840.1.113883.5.1"/>
                <birthTime value="19851210"/>
              </patient>
            </patientRole>
          </recordTarget>
          <component>
            <structuredBody>
              <component>
                <section>
                  <templateId root="2.16.840.1.113883.10.20.22.2.5.1"/>
                  <code code="11450-4" codeSystem="2.16.840.1.113883.6.1"/>
                  <title>Problems</title>
                  <entry>
                    <act>
                      <entryRelationship typeCode="SUBJ">
                        <observation>
                          <effectiveTime><low value="20240115"/></effectiveTime>
                          <value xsi:type="CD" code="N18.6" codeSystem="2.16.840.1.113883.6.90" displayName="End stage renal disease"/>
                        </observation>
                      </entryRelationship>
                    </act>
                  </entry>
                </section>
              </component>
              <component>
                <section>
                  <templateId root="2.16.840.1.113883.10.20.22.2.6.1"/>
                  <code code="48765-2" codeSystem="2.16.840.1.113883.6.1"/>
                  <title>Allergies</title>
                  <entry>
                    <act>
                      <entryRelationship typeCode="SUBJ">
                        <observation>
                          <effectiveTime><low value="20200301"/></effectiveTime>
                          <participant typeCode="CSM">
                            <participantRole>
                              <playingEntity>
                                <code code="7980" codeSystem="2.16.840.1.113883.6.88" displayName="Penicillin"/>
                              </playingEntity>
                            </participantRole>
                          </participant>
                        </observation>
                      </entryRelationship>
                    </act>
                  </entry>
                </section>
              </component>
              <component>
                <section>
                  <templateId root="2.16.840.1.113883.10.20.22.2.1.1"/>
                  <code code="10160-0" codeSystem="2.16.840.1.113883.6.1"/>
                  <title>Medications</title>
                  <entry>
                    <substanceAdministration>
                      <effectiveTime><low value="20250101"/><high value="20251231"/></effectiveTime>
                      <consumable>
                        <manufacturedProduct>
                          <manufacturedMaterial>
                            <code code="855332" codeSystem="2.16.840.1.113883.6.88" displayName="Warfarin 5 MG Oral Tablet"/>
                          </manufacturedMaterial>
                        </manufacturedProduct>
                      </consumable>
                    </substanceAdministration>
                  </entry>
                </section>
              </component>
              <component>
                <section>
                  <templateId root="2.16.840.1.113883.10.20.22.2.3.1"/>
                  <code code="30954-2" codeSystem="2.16.840.1.113883.6.1"/>
                  <title>Results</title>
                  <entry>
                    <organizer>
                      <component>
                        <observation>
                          <code code="2160-0" codeSystem="2.16.840.1.113883.6.1" displayName="Creatinine"/>
                          <effectiveTime value="20260520"/>
                          <value xsi:type="PQ" value="8.4" unit="mg/dL"/>
                        </observation>
                      </component>
                    </organizer>
                  </entry>
                </section>
              </component>
              <component>
                <section>
                  <templateId root="2.16.840.1.113883.10.20.22.2.4.1"/>
                  <code code="8716-3" codeSystem="2.16.840.1.113883.6.1"/>
                  <title>Vital Signs</title>
                  <entry>
                    <organizer>
                      <component>
                        <observation>
                          <code code="8480-6" codeSystem="2.16.840.1.113883.6.1" displayName="Systolic blood pressure"/>
                          <effectiveTime value="20260520"/>
                          <value xsi:type="PQ" value="138" unit="mm[Hg]"/>
                        </observation>
                      </component>
                    </organizer>
                  </entry>
                </section>
              </component>
              <component>
                <section>
                  <templateId root="2.16.840.1.113883.10.20.22.2.2.1"/>
                  <code code="11369-6" codeSystem="2.16.840.1.113883.6.1"/>
                  <title>Immunizations</title>
                  <entry>
                    <substanceAdministration moodCode="EVN">
                      <effectiveTime value="20251001"/>
                      <consumable>
                        <manufacturedProduct>
                          <manufacturedMaterial>
                            <code code="158" codeSystem="2.16.840.1.113883.12.292" displayName="Influenza, injectable"/>
                            <lotNumberText>LOT-2025-A</lotNumberText>
                          </manufacturedMaterial>
                        </manufacturedProduct>
                      </consumable>
                    </substanceAdministration>
                  </entry>
                </section>
              </component>
            </structuredBody>
          </component>
        </ClinicalDocument>
        """;

    public const string HeaderOnly = """
        <?xml version="1.0" encoding="UTF-8"?>
        <ClinicalDocument xmlns="urn:hl7-org:v3">
          <title>Minimal CCD</title>
          <effectiveTime value="20260101"/>
          <recordTarget>
            <patientRole>
              <id root="2.16.840.1.113883.19.5" extension="MRN-1"/>
              <patient>
                <name><given>Grace</given><family>Hopper</family></name>
              </patient>
            </patientRole>
          </recordTarget>
        </ClinicalDocument>
        """;
}
