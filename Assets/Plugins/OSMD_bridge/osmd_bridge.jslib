mergeInto(LibraryManager.library, {
	OSMD_update: function (bpm, chord_times, chord_keys, chord_count, times, keys, lengths, note_count) {
		const inputArrayUint = function (array, index) {
			return HEAPU32[(array >> 2) + index]; // see https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
		};

		const notesToXml = function (note_count, times, keys, lengths, length_per_measure, per_note_str) {
			// constants
			const sixtyfourths_per_quarter = 16;
			const semitones_from_c = [ 0, 2, 4, 5, 7, 9, 11 ];
			const semitones_per_octave = 12;
			const keys_per_octave = 7;
			console.assert(semitones_from_c.length == keys_per_octave);

			// accumulators / inter-note memory
			var time_val_prev = -1;
			var length_total = 0;
			var xml_str = '';

			// per-note
			for (var i = 0; i < note_count; ++i) {
				var time_val = inputArrayUint(times, i);
				var key_val = inputArrayUint(keys, i);
				var length_val = (lengths == null) ? sixtyfourths_per_quarter : inputArrayUint(lengths, i);
				console.log("time " + time_val + ", key " + key_val + ", length " + length_val); // TEMP?
				var is_chord = (time_val == time_val_prev);

				// measure bar if appropriate
				if (!is_chord && length_total > 0 && length_total % length_per_measure == 0) { // TODO: handle notes crossing measures?
					xml_str += '\n\
						</measure>\n\
						<measure>';
				}

				// note
				var note_semitones_from_c = key_val % semitones_per_octave;
				var note_val = semitones_from_c.indexOf(note_semitones_from_c);
				var semitone_offset = 0;
				if (note_val == -1) {
					semitone_offset = 1; // TODO: pick between sharp/flat based on major/minor key
					note_val = semitones_from_c.indexOf(note_semitones_from_c - semitone_offset);
				}
				var note_letter = String.fromCharCode(((note_val + 2) % keys_per_octave) + 'A'.charCodeAt(0)); // see https://stackoverflow.com/questions/36129721/convert-number-to-alphabet-letter
				var note_octave = Math.floor(key_val / semitones_per_octave) - 1; // NOTE offset: middle-C (MIDI key 60) in MusicXML is the start of octave 4 rather than 5
				var type_str = (length_val == 1 ? '64th' : length_val == 2 ? '32nd' : length_val == 4 ? '16th' : length_val == 8 ? 'eighth' : length_val == sixtyfourths_per_quarter ? 'quarter' : length_val == 32 ? 'half' : length_val == 64 ? 'whole' : 'ERROR');
				xml_str += '\n\
					<note>\n\
						' + (is_chord ? '<chord/>' : '') +
						'<pitch>\n\
							<step>' + note_letter + '</step>\n\
							<alter>' + semitone_offset + '</alter>\n\
							<octave>' + note_octave + '</octave>\n\
						</pitch>\n\
						<duration>' + length_val + '</duration>\n\
						<voice>1</voice>\n\
						<type>' + type_str + '</type>\n\
						<accidental>' + (semitone_offset > 0 ? 'sharp' : semitone_offset < 0 ? 'flat' : '') + '</accidental>\n'
						+ per_note_str + '\
					</note>';
				// TODO: <beam>/<dot/>/<{p/mp/mf/f}/>

				time_val_prev = time_val;
				if (!is_chord) {
					length_total += length_val;
				}
			}

			return xml_str;
		};

		// create OSMD instances if first call
		if (document.osmd_main == null) {
			document.osmd_chords = new opensheetmusicdisplay.OpenSheetMusicDisplay("osmd-chords", { drawingParameters: "compacttight" });
			document.osmd_main = new opensheetmusicdisplay.OpenSheetMusicDisplay("osmd-main", { drawingParameters: "compacttight" });
		}

		// MusicXML shared header/footer
		// TODO: use timewise rather than partwise?
		const musicxml_header = '<?xml version="1.0" encoding="UTF-8"?>\n\
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
				<part id="P1">';
		const musicxml_footer = '\n\
					</measure>\n\
				</part>\n\
			</score-partwise>';

		// chords display
		var xml_chords_str = musicxml_header + '\n\
			<measure print-object="no">\n\
				<attributes print-object="no">\n\
					<key print-object="no"></key>\n\
					<time print-object="no"></time>\n\
					<clef print-object="no">\n\
						<sign print-object="no"></sign>\n\
					</clef>\n\
				</attributes>\n\
				<direction placement="above">\n\
					<direction-type>\n\
						<words>Chord\nprogression:</words>\n\
					</direction-type>\n\
				</direction>';
		xml_chords_str += notesToXml(chord_count, chord_times, chord_keys, null, Number.POSITIVE_INFINITY, '\t\t\t\t\t\t<notehead>x</notehead>\n');
		xml_chords_str += musicxml_footer;
		console.log(xml_chords_str); // TEMP?
		document.osmd_chords.load(xml_chords_str).then(function() {
			document.osmd_chords.render();
		});

		// main display
		var xml_main_str = musicxml_header + '\n\
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
		const length_per_measure = 64; // TODO: account for different time signatures
		xml_main_str += notesToXml(note_count, times, keys, lengths, length_per_measure, '');
		xml_main_str += '\n\
			<barline location="right">\n\
				<bar-style>light-heavy</bar-style>\n\
			</barline>'
			+ musicxml_footer;
		console.log(xml_main_str); // TEMP?
		document.osmd_main.load(xml_main_str).then(function() {
			document.osmd_main.render();
		});
	},
});
