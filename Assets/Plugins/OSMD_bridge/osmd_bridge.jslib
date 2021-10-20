mergeInto(LibraryManager.library, {
	UpdateInternal: function (element_id, title, instrument_names, instrument_count, key_fifths, key_mode, note_count, times, keys, lengths, note_channels, bpm) {
		const inputArrayUint = function (array, index) {
			return HEAPU32[(array >> 2) + index]; // see https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
		};

		// create OSMD instance if necessary
		var id_str = Pointer_stringify(element_id);
		var is_untimed = (bpm == 0);
		if (document.osmds == null) {
			document.osmds = {};
		}
		if (!(id_str in document.osmds)) {
			document.osmds[id_str] = new opensheetmusicdisplay.OpenSheetMusicDisplay(id_str, { drawingParameters: is_untimed ? "compacttight" : "compact" });
		}

		// constants
		/*const*/var sixtyfourths_per_quarter = 16;
		/*const*/var semitones_from_c = [ 0, 2, 4, 5, 7, 9, 11 ];
		/*const*/var semitones_per_octave = 12;
		/*const*/var keys_per_octave = 7;
		console.assert(semitones_from_c.length == keys_per_octave);
		/*const*/var types_by_length = {
			1: ['64th', ''],
			2: ['32nd', ''],
			3: ['32nd', '<dot/>'],
			4: ['16th', ''],
			6: ['16th', '<dot/>'],
			8: ['eighth', ''],
			12: ['eighth', '<dot/>'],
			16: ['quarter', ''],
			24: ['quarter', '<dot/>'],
			32: ['half', ''],
			48: ['half', '<dot/>'],
			64: ['whole', ''],
		};

		// MusicXML header
		// TODO: use timewise rather than partwise?
		var xml_str = '<?xml version="1.0" encoding="UTF-8"?>\n\
			<!DOCTYPE score-partwise PUBLIC "-//Recordare//DTD MusicXML 2.0 Partwise//EN"\n\
				"http://www.musicxml.org/dtds/partwise.dtd">\n\
			<score-partwise version="2.0">\n\
				<part-list>';
		for (var channel_idx = 0; channel_idx < instrument_count; ++channel_idx) {
			var instrument_name_str = Pointer_stringify(inputArrayUint(instrument_names, channel_idx)); // TODO: don't assume string pointers are uints?
			xml_str += '\n\
					<score-part id="' + instrument_name_str + '">\n\
						<part-name>' + instrument_name_str + '</part-name>\n\
						<midi-instrument id="' + instrument_name_str + 'I1">\n\
							<midi-channel>' + channel_idx + '</midi-channel>\n\
							<midi-program>0</midi-program>\n\
						</midi-instrument>\n\
					</score-part>';
		}
		xml_str += '\n\
			</part-list>';

		for (var channel_idx = 0; channel_idx < instrument_count; ++channel_idx) {
			var instrument_name_str = Pointer_stringify(inputArrayUint(instrument_names, channel_idx)); // TODO: don't assume string pointers are uints?
			xml_str += '\n\
				<part id="' + instrument_name_str + '">' + (is_untimed ? '\n\
				<measure>\n\
					<attributes>\n\
						<key print-object="no"></key>\n\
						<time print-object="no"></time>\n\
						<clef>\n\
							<sign>percussion</sign>\n\
							<staff-lines>5</staff-lines>\n\
						</clef>\n\
					</attributes>\n\
					<direction placement="above">\n\
						<direction-type>\n\
							<words>' + Pointer_stringify(title) + '</words>\n\
						</direction-type>\n\
					</direction>' : '\n\
				<measure>\n\
					<attributes>\n\
						<key>\n\
							<fifths>' + key_fifths + '</fifths>\n\
							<mode>' + Pointer_stringify(key_mode) + '</mode>\n\
						</key>\n\
						<time>\n\
							<beats>4</beats>\n\
							<beat-type>4</beat-type>\n\
						</time>\n\
						<clef number="1">\n\
							<sign>G</sign>\n\
							<line>2</line>\n\
						</clef>\n\
						<divisions>16</divisions>/*TODO: base on time signature?*/\n\
					</attributes>\n\
					<direction placement="above">\n\
						<direction-type>\n\
							<metronome>\n\
								<beat-unit>quarter</beat-unit>\n\
								<per-minute>' + bpm + '</per-minute>\n\
							</metronome>\n\
						</direction-type>\n\
						<sound tempo="' + bpm + '"/>\n\
					</direction>');

			var length_per_measure = (is_untimed ? Number.POSITIVE_INFINITY : 64); // TODO: account for different time signatures
			var per_note_str = (is_untimed ? '\t\t\t\t\t\t\t<notehead>x</notehead>\n' : '');

			// accumulators / inter-note memory
			var time_val_prev = -1;
			var length_val_prev = -1;
			var overlap_amount = 0; // TODO: support more than two overlapping voices?
			var beam_before = false;

			// per-note
			for (var i = 0; i < note_count; ++i) {
				var channel_val = inputArrayUint(note_channels, i);
				if (channel_val != channel_idx) {
					continue;
				}

				var time_vals = [ inputArrayUint(times, i) ];
				var key_vals = [ inputArrayUint(keys, i) ];
				var length_vals = [ inputArrayUint(lengths, i) ];
				console.log("time " + time_vals[0] + ", key " + key_vals[0] + ", length " + length_vals[0] + ", channel " + channel_val); // TEMP?

				var tied_chord_count = 0;
				for (var tied_idx = 0; tied_idx < length_vals.length; ++tied_idx) {
					// split if crossing a measure boundary
					var time_val_next = time_vals[tied_idx] + length_vals[tied_idx];
					var length_post = parseInt(time_val_next) % length_per_measure;
					while ((length_post > 0 && length_post < length_vals[tied_idx]) || !(length_vals[tied_idx] in types_by_length)) {
						if (length_post == length_vals[tied_idx]) {
							var length_itr;
							for (length_itr = length_vals[tied_idx]; length_itr > 0 && !(length_itr in types_by_length); --length_itr);
							length_post = length_itr;
						}
						length_vals[tied_idx] -= length_post;
						time_val_next -= length_post;
						for (; time_val_next > inputArrayUint(times, i + 1); ++i) {
							if (inputArrayUint(note_channels, i + 1) != channel_val) {
								continue;
							}
							length_vals.splice(tied_idx, 0, inputArrayUint(lengths, i + 1));
							key_vals.splice(tied_idx, 0, inputArrayUint(keys, i + 1));
							time_vals.splice(tied_idx, 0, inputArrayUint(times, i + 1));
						}
						length_vals.splice(tied_idx + 1, 0, length_post);
						key_vals.splice(tied_idx + 1, 0, key_vals[tied_idx]);
						time_vals.splice(tied_idx + 1, 0, time_val_next);
						++tied_chord_count;
					}

					var is_chord = (time_vals[tied_idx] == time_val_prev && length_vals[tied_idx] == length_val_prev);

					// overlap w/ previous note(s) if necessary
					if (!is_chord && time_vals[tied_idx] < time_val_prev + length_val_prev) {
						overlap_amount = time_val_prev + length_val_prev - time_vals[tied_idx];
						xml_str += '\n\
							<backup>\n\
								<duration>' + overlap_amount + '</duration>\n\
							</backup>';
					}

					var length_val = length_vals[tied_idx];
					var key_val = key_vals[tied_idx];
					var time_val = time_vals[tied_idx];

					// measure bar if appropriate
					var new_measure = (!is_chord && overlap_amount <= 0 && time_val > 0 && time_val % length_per_measure == 0);
					if (new_measure) {
						xml_str += '\n\
							</measure>\n\
							<measure>';
					}

					// note
					var pitch_tag = (is_untimed ? 'unpitched' : 'pitch');
					var pitch_prefix = (is_untimed ? 'display-' : '');
					var note_semitones_from_c = key_val % semitones_per_octave;
					var note_val = semitones_from_c.indexOf(note_semitones_from_c);
					var semitone_offset = 0;
					if (note_val == -1) {
						semitone_offset = (key_fifths < 0) ? -1 : 1;
						note_val = semitones_from_c.indexOf(note_semitones_from_c - semitone_offset);
					}
					var note_letter = String.fromCharCode(((note_val + 2) % keys_per_octave) + 'A'.charCodeAt(0)); // see https://stackoverflow.com/questions/36129721/convert-number-to-alphabet-letter
					var note_octave = Math.floor(key_val / semitones_per_octave) - 1; // NOTE offset: middle-C (MIDI key 60) in MusicXML is the start of octave 4 rather than 5
					var type_and_dot_str = (length_val in types_by_length) ? types_by_length[length_val] : [ 'ERROR', '' ];
					var type_str = type_and_dot_str[0];
					var dot_str = type_and_dot_str[1];
					beam_before = !new_measure && (is_chord ? beam_before : i > 0 && length_val <= 8 && length_val_prev == length_val);
					var j = i + 1;
					for (; j < note_count && length_val <= 8 && (inputArrayUint(times, j) == time_val || inputArrayUint(note_channels, j) != channel_val); ++j) {} // TODO: better channel filtering?
					var beam_after = (j < note_count && length_val <= 8 && inputArrayUint(lengths, j) <= 8 && inputArrayUint(note_channels, j) == channel_val); // TODO: detect measure end?
					var beam_str = (beam_before && beam_after) ? 'continue' : (beam_before ? 'end' : (beam_after ? 'begin' : ''));
					var accidental_str = (semitone_offset > 0 ? 'sharp' : semitone_offset < 0 ? 'flat' : '');
					var tie_type_str = (tied_idx < length_vals.length / (tied_chord_count + 1) ? 'start' : (tied_idx >= length_vals.length - length_vals.length / (tied_chord_count + 1) ? 'stop' : 'stop"/><tied type="start'));
					xml_str += '\n\
						<note>\n\
							' + (is_chord ? '<chord/>' : '') +
							'<' + pitch_tag + '>\n\
								<' + pitch_prefix + 'step>' + note_letter + '</' + pitch_prefix + 'step>\n\
								<alter>' + semitone_offset + '</alter>\n\
								<' + pitch_prefix + 'octave>' + note_octave + '</' + pitch_prefix + 'octave>\n\
							</' + pitch_tag + '>\n\
							<duration>' + length_val + '</duration>\n\
							<voice>' + (overlap_amount > 0 ? 2 : 1) + '</voice>\n\
							<type>' + type_str + '</type>' + dot_str + '\n'
							+ (beam_str == '' ? '' : '\t\t\t\t\t\t\t<beam number="1">' + beam_str + '</beam>\n')
							+ (accidental_str == '' ? '' : ('\t\t\t\t\t\t\t<accidental>' + accidental_str + '</accidental>\n'))
							+ per_note_str
							+ (length_vals.length > 1 ? '\t\t\t\t\t\t\t<notations><tied type="' + tie_type_str + '"/></notations>\n' : '') + '\
						</note>';
					// TODO: <{p/mp/mf/f}/>?

					time_val_prev = time_val;
					length_val_prev = length_val;
					overlap_amount -= length_val;
				}
			}

			if (!is_untimed) {
				xml_str += '\n\
					<barline location="right">\n\
						<bar-style>light-heavy</bar-style>\n\
					</barline>';
			}
			xml_str += '\n\
					</measure>\n\
				</part>';
		}

		// footer
		xml_str += '\n\
			</score-partwise>';

		// output
		console.log(xml_str); // TEMP?
		document.osmds[id_str].load(xml_str).then(function() {
			document.osmds[id_str].render();
		});
	},
});
