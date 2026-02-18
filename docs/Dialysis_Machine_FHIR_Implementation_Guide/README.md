# FHIR Implementation Guide for Dialysis PDMS

## Overview

This folder hosts the formal FHIR Implementation Guide (IG) for dialysis machines when published by the [Dialysis Interoperability Consortium](https://www.dialysisinterop.org/) or HL7. The PDMS uses FHIR R4 as its primary interoperability model per project goals.

## Links to Published IGs

- **Dialysis Interop Consortium**: [dialysisinterop.org](https://www.dialysisinterop.org/) – FHIR IG in development (since April 2023)
- **HL7 FHIR IGs**: When the formal dialysis machine FHIR IG is published by HL7, the link will be added here

## QI-Core Hemodialysis Machine Observation Example

Reference: [QI-Core NonPatient Hemodialysis Machine Observation Example](https://build.fhir.org/ig/HL7/fhir-qi-core/Observation-example-nonpatient-hemodialysis-machine.html)

This example demonstrates the QI-Core NonPatient Observation pattern for device observations (e.g., UF rate, venous pressure) where the `focus` is a `Device` rather than a `Patient`.

## FHIR Resources for Dialysis

| Resource       | Use                                                                 |
| -------------- | ------------------------------------------------------------------- |
| `Device`      | Dialysis machine identity (UDI, manufacturer, model, serial)        |
| `Observation` | Treatment data (UF rate, pressures, conductivity, blood leak, etc.) |
| `ServiceRequest` | Prescription; therapy modality, UF target, flow rates            |
| `Procedure`   | Dialysis session; status, performedPeriod                           |
| `DetectedIssue` | Clinical alarms requiring action                                 |
| `Provenance`  | Setting origin (RSET/MSET/ASET)                                     |

## Firely SDK Usage Notes for PDMS

The PDMS uses the [Firely .NET SDK](https://fire.ly/products/firely-net-sdk/) for FHIR:

- **Validation**: Use `Hl7.Fhir.Validation` for resource validation
- **Serialization**: FHIR JSON/XML via Firely serializer
- **API client**: Build FHIR `Observation`, `Device`, `Procedure`, etc. for persistence and HL7-to-FHIR mapping

See project configuration for Firely package references and usage.

## References

- [FHIR R4 Observation](https://hl7.org/fhir/R4/observation.html)
- [FHIR R4 Device](https://hl7.org/fhir/R4/device.html)
- [HL7 v2 Guide](../Dialysis_Machine_HL7_Implementation_Guide/) – Current HL7 specification
