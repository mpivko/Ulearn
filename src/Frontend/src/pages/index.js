import { Component } from "react";
import React from "react";
import * as PropTypes from "prop-types";
import styles from "./pages.less"

export class Page extends Component {
	classes = {
		'wide': styles.wideContainerWrapper,
		'slide': styles.slideContainerWrapper,
		'common': styles.contentWrapper,
	};

	componentDidMount() {
		window.scrollTo(0, 0);
	}

	render() {
		return (
			<div className={styles.wrapper}>
				<div className={this.classes[this.props.width || 'common']}>
					{ this.props.children }
				</div>
			</div>
		)
	}

	static propTypes = {
		width: PropTypes.string,
	};
}
