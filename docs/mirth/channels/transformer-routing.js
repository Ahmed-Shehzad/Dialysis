// Mirth Connect transformer: route HL7 to Dialysis PDMS by message type.
// Paste into the destination connector's transformer step.
// Requires channel variables: pdmsBaseUrl (or patientBaseUrl, prescriptionBaseUrl, treatmentBaseUrl, alarmBaseUrl).

var hl7 = connectorMessage.getRawData();
if (!hl7 || hl7.length === 0) {
    throw new Error('No message data');
}

var msh9 = getSegment(hl7, 'MSH', 0, 9);
var msgType = (msh9 || '').split('^').slice(0, 2).join('^');

var base = $('pdmsBaseUrl') || $('patientBaseUrl') || 'http://localhost:5001';
var patientBase = $('patientBaseUrl') || base;
var prescriptionBase = $('prescriptionBaseUrl') || base;
var treatmentBase = $('treatmentBaseUrl') || base;
var alarmBase = $('alarmBaseUrl') || base;

var url, body;

if (msgType === 'QBP^Q22') {
    url = patientBase + '/api/hl7/qbp-q22';
    body = JSON.stringify({ rawHl7Message: hl7 });
} else if (msgType === 'RSP^K22') {
    var rspTarget = $('rspK22Target') || 'patient';
    var baseForRsp = rspTarget === 'prescription' ? prescriptionBase : patientBase;
    url = baseForRsp + (rspTarget === 'prescription' ? '/api/prescriptions/hl7/rsp-k22' : '/api/hl7/rsp-k22');
    body = JSON.stringify({ rawHl7Message: hl7 });
} else if (msgType === 'QBP^D01') {
    url = prescriptionBase + '/api/hl7/qbp-d01';
    body = JSON.stringify({ rawHl7Message: hl7 });
} else if (msgType === 'ORU^R01') {
    url = treatmentBase + '/api/hl7/oru';
    body = JSON.stringify({ rawHl7Message: hl7 });
} else if (msgType === 'ORU^R40') {
    url = alarmBase + '/api/hl7/alarm';
    body = JSON.stringify({ rawHl7Message: hl7 });
} else if (hl7.indexOf('FHS|') === 0) {
    url = treatmentBase + '/api/hl7/oru/batch';
    body = JSON.stringify({ rawHl7Batch: hl7 });
} else {
    throw new Error('Unknown message type: ' + msgType);
}

channelMap.put('targetUrl', url);
connectorMessage.setRawData(body);

function getSegment(hl7Str, segId, index, fieldNum) {
    var segs = hl7Str.split('\r').filter(function(s) { return s.indexOf(segId + '|') === 0; });
    var seg = segs[index];
    if (!seg) return null;
    var fields = seg.split('|');
    return fields[fieldNum] || null;
}
