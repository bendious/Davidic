mergeInto(LibraryManager.library, {
	OSMD_update: function (bpm, keys, lengths, key_count) {
		// create OSMD instance if first call
		if (document.osmd == null) {
			document.osmd = new opensheetmusicdisplay.OpenSheetMusicDisplay("osmd", { drawingParameters: "compacttight" });
		}

		// MusicXML header
		// TODO: use timewise rather than partwise?
		var xml_str = '<?xml version="1.0" encoding="UTF-8"?>\n\
			<!DOCTYPE score-partwise PUBLIC "-//Recordare//DTD MusicXML 2.0 Partwise//EN"\n\
			  "http://www.musicxml.org/dtds/partwise.dtd">\n\
			<score-partwise version="2.0">\n\
			  <part-list>\n\
				<score-part id="P1">\n\
				  <midi-instrument id="P1I1">\n\
					<midi-channel>0</midi-channel>\n\
					<midi-program>0</midi-program>\n\
				  </midi-instrument>\n\
				</score-part>\n\
			  </part-list>\n\
			  <part id="P1">\n\
				<measure>\n\
				  <attributes>\n\
					<key>\n\
					  <fifths>0</fifths>\n\
					  <mode>major</mode>\n\
					</key>\n\
					<time>\n\
					  <beats>4</beats>\n\
					  <beat-type>4</beat-type>\n\
					</time>\n\
					<clef number="1">\n\
					  <sign>G</sign>\n\
					  <line>2</line>\n\
					</clef>\n\
				  </attributes>\n\
				  <direction placement="above">\n\
					<direction-type>\n\
					  <metronome>\n\
						<beat-unit>quarter</beat-unit>\n\
						<per-minute>' + bpm + '</per-minute>\n\
					  </metronome>\n\
					</direction-type>\n\
					<sound tempo="' + bpm + '"/>\n\
				  </direction>';

		// convert key array into MusicXML
		const length_per_measure = 64; // TODO: account for different time signatures
		var length_total = 0;
		var length_val_prev = 0;
		var type_str = '';
		for (var i = 0; i < key_count; ++i) {
			var key_val = HEAPU32[(keys >> 2) + i]; // see https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
			var length_val = HEAPU32[(lengths >> 2) + i];
			console.log("key " + key_val + ", length " + length_val); // TEMP?

			// measure bar if appropriate
			if (length_total > 0 && length_val > 0 && length_total % length_per_measure == 0) { // TODO: handle notes crossing measures?
				xml_str += '\n\
				  </measure>\n\
				  <measure>';
			}

			// note
			const semitones_from_c = [ 0, 2, 4, 5, 7, 9, 11 ];
			const semitones_per_octave = 12;
			const keys_per_octave = 7;
			console.assert(semitones_from_c.length == keys_per_octave);
			var note_semitones_from_c = key_val % semitones_per_octave;
			var note_val = semitones_from_c.indexOf(note_semitones_from_c);
			var semitone_offset = 0;
			if (note_val == -1) {
				semitone_offset = 1; // TODO: pick between sharp/flat based on major/minor key
				note_val = semitones_from_c.indexOf(note_semitones_from_c - semitone_offset);
			}
			var note_letter = String.fromCharCode(((note_val + 2) % keys_per_octave) + 'A'.charCodeAt(0)); // see https://stackoverflow.com/questions/36129721/convert-number-to-alphabet-letter
			var note_octave = key_val / semitones_per_octave - 1; // NOTE offset: middle-C (MIDI key 60) in MusicXML is the start of octave 4 rather than 5
			type_str = (length_val == 1 ? '64th' : length_val == 2 ? '32nd' : length_val == 4 ? '16th' : length_val == 8 ? 'eighth' : length_val == 16 ? 'quarter' : length_val == 32 ? 'half' : length_val == 64 ? 'whole' : type_str); // note that length_val of 0 is used for subsequent chord notes, in which case we just reuse the previous type
			xml_str += '\n\
				  <note>\n\
					' + (length_val == 0 ? '<chord/>\n' : '') + '\
					<pitch>\n\
					  <step>' + note_letter + '</step>\n\
					  <alter>' + semitone_offset + '</alter>\n\
					  <octave>' + note_octave + '</octave>\n\
					</pitch>\n\
					<duration>' + (length_val == 0 ? length_val_prev : length_val) + '</duration>\n\
					<voice>1</voice>\n\
					<type>' + type_str + '</type>\n\
					<accidental>' + (semitone_offset > 0 ? 'sharp' : semitone_offset < 0 ? 'flat' : '')/*TODO: account for key signature*/ + '</accidental>\n\
				  </note>';
			// TODO: <beam>/<dot/>/<{p/mp/mf/f}/>

			length_total += length_val;
			length_val_prev = length_val;
		}

		// MusicXML footer
		xml_str += '\n\
				  <barline location="right">\n\
					<bar-style>light-heavy</bar-style>\n\
				  </barline>\n\
				</measure>\n\
			  </part>\n\
			</score-partwise>';

		// TEMP?
		console.log(xml_str);

		// load and render
		document.osmd.load(xml_str).then(function() {
			document.osmd.render();
		});
	},
});
